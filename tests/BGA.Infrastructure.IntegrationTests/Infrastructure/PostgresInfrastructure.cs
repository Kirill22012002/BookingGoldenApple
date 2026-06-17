using BGA.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BGA.Infrastructure.IntegrationTests.Infrastructure;

public class PostgresInfrastructure : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres
        = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.Migrate();
        return context;
    }

    protected async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"catalog\".\"bookings\", \"catalog\".\"events\" RESTART IDENTITY CASCADE");
    }
}
