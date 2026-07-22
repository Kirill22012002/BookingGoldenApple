using BGA.Users.Application.Services.Implementations;
using BGA.Users.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BGA.Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
