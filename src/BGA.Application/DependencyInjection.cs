using BGA.Application.BackgroundServices;
using BGA.Application.Services.Implementations;
using BGA.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BGA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddSingleton(TimeProvider.System);

        services.AddHostedService<BookingProcessingService>();

        return services;
    }
}
