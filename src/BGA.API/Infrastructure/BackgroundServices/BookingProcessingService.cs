using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace BGA.API.Infrastructure.BackgroundServices;

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
            using var scope = _serviceScopeFactory.CreateAsyncScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            try
            {
                var pendingBookings = await bookingRepository.GetAllInPendingAsync(stoppingToken);
                var tasks = pendingBookings.Select(booking => SimulateLatency(bookingService.ProcessBookingAsync, booking, stoppingToken));
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

    private async Task SimulateLatency(Func<Booking, CancellationToken, Task> action, Booking booking, CancellationToken stoppingToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingDelaySec), stoppingToken);
        await action(booking, stoppingToken);
    }
}
