using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Infrastructure.BackgroundServices;

public class BookingProcessingService(
    ILogger<BookingProcessingService> _logger,
    IServiceScopeFactory _serviceScopeFactory) : BackgroundService
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateAsyncScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            try
            {
                var pendingBookings = await bookingRepository.GetAllInPendingAsync(stoppingToken);
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing booking");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        using var scope = _serviceScopeFactory.CreateAsyncScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        await _processingSemaphore.WaitAsync(cancellationToken);
        try
        {
            var eventExists = await eventRepository.ExistsAsync(booking.EventId, cancellationToken);
            if (eventExists)
            {
                booking.Confirm();
            }
            else
            {
                booking.Reject();
                _logger.LogWarning("Error while processing booking because Event not exist. EventId {EventId}, BookingId {}", booking.EventId, booking.Id);
            }
        }
        catch (Exception ex)
        {
            booking.Reject();
            var @event = await eventRepository.GetByIdAsync(booking.EventId, cancellationToken);
            if (@event != null)
            {
                @event.ReleaseSeats();
                await eventRepository.UpdateAsync(@event, cancellationToken);
            }

            _logger.LogWarning(ex, "Error while processing booking. BookingId {BookingId}", booking.Id);
        }
        finally
        {
            await bookingRepository.UpdateAsync(booking, cancellationToken);
            _processingSemaphore.Release();
        }
    }
}
