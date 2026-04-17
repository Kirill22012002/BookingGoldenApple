using BGA.API.Application;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Infrastructure.BackgroundServices;

public class BookingProcessingService(
    ILogger<BookingProcessingService> _logger,
    IServiceScopeFactory _serviceScopeFactory) : BackgroundService
{
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
                var tasks = pendingBookings.Select(booking => SimulateLatency(bookingService.ProcessBookingAsync, booking, simulatedLatencySec: 2, stoppingToken));
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

    private async Task<ServiceResponse> SimulateLatency(Func<Booking, CancellationToken, Task<ServiceResponse>> action, Booking booking, int simulatedLatencySec, CancellationToken stoppingToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(simulatedLatencySec), stoppingToken);
        return await action(booking, stoppingToken);
    }
}
