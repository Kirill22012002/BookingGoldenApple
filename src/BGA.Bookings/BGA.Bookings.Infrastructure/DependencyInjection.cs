using BGA.Bookings.Application.Messaging;
using BGA.Bookings.Application.Repositories;
using BGA.Bookings.Infrastructure.DataAccess;
using BGA.Bookings.Infrastructure.DataAccess.Repositories;
using BGA.Bookings.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BGA.Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContext<BookingsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IBookingConfirmedPublisher, KafkaBookingConfirmedPublisher>();

        return services;
    }

    public static void ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        context.Database.Migrate();
    }
}
