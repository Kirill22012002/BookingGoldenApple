using BGA.Events.Domain.Models;
using BGA.Events.Infrastructure.DataAccess.Repositories;
using BGA.Events.Infrastructure.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BGA.Events.Infrastructure.IntegrationTests;

public class EventRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetAll_ReturnsAllEvents()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var event1 = CreateEvent("Morning yoga", new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero));
        var event2 = CreateEvent("Tech meetup", new DateTimeOffset(2026, 6, 2, 18, 0, 0, TimeSpan.Zero));
        var event3 = CreateEvent("Board games night", new DateTimeOffset(2026, 6, 3, 19, 0, 0, TimeSpan.Zero));
        await context.Events.AddRangeAsync([event1, event2, event3], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(CreateContext());
        var result = await repository.GetAll(null, null, null).OrderBy(@event => @event.StartAt).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.Equal("Morning yoga", result[0].Title);
        Assert.Equal("Tech meetup", result[1].Title);
        Assert.Equal("Board games night", result[2].Title);
    }

    [Fact]
    public async Task GetAll_WhenFilterByTitle_ReturnsOnlyMatchingEvents()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var events = new[]
        {
            CreateEvent("Jogging", new DateTimeOffset(2026, 3, 26, 0, 0, 0, TimeSpan.Zero)),
            CreateEvent("Running", new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero)),
            CreateEvent("Theatre", new DateTimeOffset(2026, 3, 26, 0, 0, 0, TimeSpan.Zero)),
            CreateEvent("JUMPING", new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero))
        };
        await context.Events.AddRangeAsync(events, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(CreateContext());
        var result = await repository.GetAll("ing", null, null).OrderBy(@event => @event.Title).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.Equal(["JUMPING", "Jogging", "Running"], result.Select(@event => @event.Title).ToArray());
    }

    [Fact]
    public async Task GetAll_WhenFilterByFrom_ReturnsOnlyEventsWithStartAtGreaterOrEqual()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var events = new[]
        {
            CreateEvent("Event 1", new DateTimeOffset(2026, 3, 14, 0, 0, 0, TimeSpan.Zero)),
            CreateEvent("Event 2", new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero)),
            CreateEvent("Event 3", new DateTimeOffset(2026, 3, 16, 0, 0, 0, TimeSpan.Zero))
        };
        await context.Events.AddRangeAsync(events, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var from = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetAll(null, from, null).OrderBy(@event => @event.StartAt).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, @event => Assert.True(@event.StartAt >= from));
    }

    [Fact]
    public async Task GetAll_WhenFilterByTo_ReturnsOnlyEventsWithEndAtLessOrEqual()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var events = new[]
        {
            CreateEvent("Event 1", new DateTimeOffset(2026, 3, 13, 0, 0, 0, TimeSpan.Zero)),
            CreateEvent("Event 2", new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero)),
            CreateEvent("Event 3", new DateTimeOffset(2026, 3, 16, 0, 0, 0, TimeSpan.Zero))
        };
        await context.Events.AddRangeAsync(events, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var to = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);
        var repository = new EventRepository(CreateContext());
        var result = await repository.GetAll(null, null, to).OrderBy(@event => @event.EndAt).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.All(result, @event => Assert.True(@event.EndAt <= to));
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ReturnsSingleEvent()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = CreateEvent("Summer party", new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.Zero), 40);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(CreateContext());
        var result = await repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(@event.Id, result.Id);
        Assert.Equal(@event.Title, result.Title);
    }

    [Fact]
    public async Task ExistsAsync_WhenEventExists_ReturnsTrue()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = CreateEvent("Summer party", new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.Zero), 40);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(CreateContext());
        var result = await repository.ExistsAsync(@event.Id, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task CreateAsync_SavesEventToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var repository = new EventRepository(context);
        var @event = CreateEvent("Spring meetup", new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero), 25);

        await repository.CreateAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstOrDefaultAsync(entity => entity.Id == @event.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("Spring meetup", saved.Title);
    }

    [Fact]
    public async Task Update_WhenReschedule_UpdatesEventInDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = CreateEvent("Conference", new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), 100);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newStartAt = new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero);
        var newEndAt = new DateTimeOffset(2026, 7, 11, 13, 0, 0, TimeSpan.Zero);
        @event.Reschedule(newStartAt, newEndAt);

        new EventRepository(context).Update(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstOrDefaultAsync(entity => entity.Id == @event.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(newStartAt, saved.StartAt);
        Assert.Equal(newEndAt, saved.EndAt);
    }

    [Fact]
    public async Task Remove_RemovesEventFromDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = CreateEvent("To remove", new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero));
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        new EventRepository(context).Remove(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var deleted = await verifyContext.Events.FirstOrDefaultAsync(entity => entity.Id == @event.Id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    private static Event CreateEvent(string title, DateTimeOffset startAt, int totalSeats = 10)
    {
        return new Event(title, null, startAt, startAt.AddHours(2), totalSeats);
    }
}
