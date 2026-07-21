using BGA.Application.Repositories;
using BGA.Application.Services.Interfaces;
using BGA.Application.Settings;
using BGA.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BGA.Application.BackgroundServices;

public class BookingProcessingService(
    ILogger<BookingProcessingService> _logger,
    IServiceScopeFactory _serviceScopeFactory,
    IOptions<ApplicationSettingsOptions> options) : BackgroundService
{
    private readonly ApplicationSettingsOptions _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IEnumerable<Booking> pendingBookings;
            await using (var readScope = _serviceScopeFactory.CreateAsyncScope())
            {
                var bookingRepository = readScope.ServiceProvider.GetRequiredService<IBookingRepository>();
                pendingBookings = await bookingRepository.GetAllInPendingAsync(stoppingToken);
            }

            var tasks = pendingBookings.Select(booking => ProcessBookingSafelyAsync(booking, stoppingToken));

            try
            {
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

            await Task.Delay(TimeSpan.FromSeconds(_settings.PoolingIntervalSec), stoppingToken);
        }
    }

    private async Task ProcessBookingSafelyAsync(Booking booking, CancellationToken stoppingToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        try
        {
            await SimulateLatency(bookingService.ProcessBookingAsync, booking, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process booking {BookingId}", booking.Id);
        }
    }

    private async Task SimulateLatency(Func<Booking, CancellationToken, Task> action, Booking booking, CancellationToken stoppingToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingDelaySec), stoppingToken);
        await action(booking, stoppingToken);
    }
}
