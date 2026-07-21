using BGA.Bookings.Application.BackgroundServices;
using BGA.Bookings.Application.Services.Implementations;
using BGA.Bookings.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BGA.Bookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<BookingProcessingService>();

        return services;
    }
}
