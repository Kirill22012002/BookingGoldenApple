using BGA.Events.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Testcontainers.PostgreSql;

namespace BGA.Events.API.E2ETests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public HttpClient HttpClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        HttpClient = CreateClient();
    }

    public new async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

    public EventsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new EventsDbContext(options);
        context.Database.Migrate();
        return context;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"events\" RESTART IDENTITY CASCADE");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var optionsDescriptor = services.SingleOrDefault(service => service.ServiceType == typeof(DbContextOptions<EventsDbContext>));
            if (optionsDescriptor is not null)
            {
                services.Remove(optionsDescriptor);
            }

            var connectionDescriptor = services.SingleOrDefault(service => service.ServiceType == typeof(DbConnection));
            if (connectionDescriptor is not null)
            {
                services.Remove(connectionDescriptor);
            }

            services.AddDbContext<EventsDbContext>((_, option) => option.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
