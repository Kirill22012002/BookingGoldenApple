using BGA.API.Infrastructure.DataAccess.Repositories.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.IntegrationTests;

public class EventRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetAllAsync_()
    {

    }

    [Fact]
    public async Task GetByIdAsync_()
    {

    }

    [Fact]
    public async Task ExistsAsync_()
    {

    }

    [Fact]
    public async Task CreateAsync_SavesEventToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);
        var startAt = new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero);
        var endAt = new DateTimeOffset(2026, 03, 15, 12, 0, 0, TimeSpan.Zero);
        var @event = new Event("Spring meetup", "Integration test event", startAt, endAt, 25);

        // Act
        await repository.CreateAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == @event.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal("Spring meetup", saved.Title);
        Assert.Equal("Integration test event", saved.Description);
        Assert.Equal(startAt, saved.StartAt);
        Assert.Equal(endAt, saved.EndAt);
        Assert.Equal(25, saved.TotalSeats);
        Assert.Equal(25, saved.AvailableSeats);
    }

    [Fact]
    public void Update()
    {

    }

    [Fact]
    public async Task Remove_RemovesEventFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var @event = new Event(
            "To remove",
            "Will be deleted by repository",
            new DateTimeOffset(2026, 04, 01, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 04, 01, 12, 0, 0, TimeSpan.Zero),
            10);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);

        // Act
        repository.Remove(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var deleted = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == @event.Id, TestContext.Current.CancellationToken);

        Assert.Null(deleted);
    }
}
