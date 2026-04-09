using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Infrastructure.BackgroundServices;

public class BookingProcessingService(
    ILogger<BookingProcessingService> _logger,
    IServiceScopeFactory _serviceScopeFactory,
    TimeProvider _timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateAsyncScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            try
            {
                var bookingsInPending = await bookingRepository.GetAllInPendingAsync(stoppingToken);
                foreach (var booking in bookingsInPending)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                    try
                    {
                        booking.Status = BookingStatus.Confirmed;
                        booking.ProcessedAt = _timeProvider.GetUtcNow();
                        await bookingRepository.UpdateAsync(booking, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while processing booking. BookingId {BookingId}", booking.Id);
                    }
                }
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
}
