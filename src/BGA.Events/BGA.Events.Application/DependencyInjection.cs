using BGA.Events.Application.Services.Implementations;
using BGA.Events.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BGA.Events.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        return services;
    }
}
