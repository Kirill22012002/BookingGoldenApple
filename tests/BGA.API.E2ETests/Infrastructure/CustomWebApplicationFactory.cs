using BGA.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using Testcontainers.PostgreSql;

namespace BGA.API.E2ETests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres
        = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public HttpClient HttpClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        HttpClient = CreateClient();
    }

    public new async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.Migrate();
        return context;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"catalog\".\"bookings\", \"catalog\".\"events\" RESTART IDENTITY CASCADE");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.SingleOrDefault(service => typeof(DbContextOptions<ApplicationDbContext>) == service.ServiceType)!);
            services.Remove(services.SingleOrDefault(service => typeof(DbConnection) == service.ServiceType)!);
            services.AddDbContext<ApplicationDbContext>((_, option) => option.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
