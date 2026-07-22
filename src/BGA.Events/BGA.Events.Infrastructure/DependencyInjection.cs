using BGA.Events.Application.Caching;
using BGA.Events.Application.Repositories;
using BGA.Events.Infrastructure.Configuration;
using BGA.Events.Infrastructure.Caching;
using BGA.Events.Infrastructure.DataAccess;
using BGA.Events.Infrastructure.DataAccess.Repositories;
using BGA.Events.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace BGA.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");
        var redisOptions = RedisConfigurationOptionsFactory.Create(configuration);

        services.AddDbContext<EventsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<KafkaTopicInitializerHostedService>();
        services.AddHostedService<BookingConfirmedConsumerHostedService>();

        return services;
    }

    public static void ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        context.Database.Migrate();
    }
}
