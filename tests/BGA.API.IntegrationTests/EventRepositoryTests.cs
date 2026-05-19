using BGA.API.Infrastructure.DataAccess.Repositories.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.IntegrationTests;

public class EventRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetAll_ReturnsAllEvents()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var event1 = new Event(
            "Morning yoga",
            "City park session",
            new DateTimeOffset(2026, 06, 01, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 01, 9, 0, 0, TimeSpan.Zero),
            15);
        var event2 = new Event(
            "Tech meetup",
            "Evening networking",
            new DateTimeOffset(2026, 06, 02, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 02, 20, 0, 0, TimeSpan.Zero),
            50);
        var event3 = new Event(
            "Board games night",
            "Community center event",
            new DateTimeOffset(2026, 06, 03, 19, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 03, 22, 0, 0, TimeSpan.Zero),
            20);
        await context.Events.AddRangeAsync([event1, event2, event3], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(CreateContext());

        // Act
        var result = repository.GetAll();
        var events = await result
            .OrderBy(e => e.StartAt)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(events);
        Assert.Equal(3, events.Count);
        Assert.Equal("Morning yoga", events[0].Title);
        Assert.Equal("Tech meetup", events[1].Title);
        Assert.Equal("Board games night", events[2].Title);
    }

    [Fact]
    public async Task GetAll_ReturnsEntitiesWithoutTrackingChanges()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var seedContext = CreateContext();
        var @event = new Event(
            "Original title",
            "No tracking check",
            new DateTimeOffset(2026, 06, 10, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 10, 14, 0, 0, TimeSpan.Zero),
            30);
        await seedContext.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var result = repository.GetAll();
        var loadedEvent = await result.SingleAsync(TestContext.Current.CancellationToken);
        loadedEvent.Title = "Changed title";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(context.ChangeTracker.Entries());

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .SingleAsync(e => e.Id == @event.Id, TestContext.Current.CancellationToken);

        Assert.Equal("Original title", saved.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ReturnsSingleEvent()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var startAt = new DateTimeOffset(2026, 06, 10, 18, 0, 0, TimeSpan.Zero);
        var endAt = new DateTimeOffset(2026, 06, 10, 20, 0, 0, TimeSpan.Zero);
        var @event = new Event("Summer party", "Open air event", startAt, endAt, 40);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(CreateContext());

        // Act
        var result = await repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Summer party", result.Title);
        Assert.Equal("Open air event", result.Description);
        Assert.Equal(startAt, result.StartAt);
        Assert.Equal(endAt, result.EndAt);
        Assert.Equal(40, result.TotalSeats);
        Assert.Equal(40, result.AvailableSeats);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventNotExists_ReturnsNull()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(CreateContext());
        var notExistsId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(notExistsId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenEventExists_ReturnsTrue()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var startAt = new DateTimeOffset(2026, 06, 10, 18, 0, 0, TimeSpan.Zero);
        var endAt = new DateTimeOffset(2026, 06, 10, 20, 0, 0, TimeSpan.Zero);
        var @event = new Event("Summer party", "Open air event", startAt, endAt, 40);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(CreateContext());

        // Act
        var result = await repository.ExistsAsync(@event.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenEventNotExists_ReturnsFalse()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(CreateContext());
        var notExistsId = Guid.NewGuid();

        // Act
        var result = await repository.ExistsAsync(notExistsId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
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
    public async Task Query_WhenIncludingBookingsForEvent_LoadsRelatedBookings()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var @event = new Event(
            "Backend conference",
            "Distributed systems",
            new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 15, 18, 0, 0, TimeSpan.Zero),
            100);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var booking1 = new Booking(
            @event.Id,
            BookingStatus.Pending,
            new DateTimeOffset(2026, 11, 01, 10, 0, 0, TimeSpan.Zero));
        var booking2 = new Booking(
            @event.Id,
            BookingStatus.Confirmed,
            new DateTimeOffset(2026, 11, 02, 11, 0, 0, TimeSpan.Zero));
        await context.Bookings.AddRangeAsync([booking1, booking2], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await using var verifyContext = CreateContext();
        var loadedEvent = await verifyContext.Events
            .Include(e => e.Bookings)
            .SingleAsync(e => e.Id == @event.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(loadedEvent.Bookings);
        Assert.Equal(2, loadedEvent.Bookings.Count);
        Assert.All(loadedEvent.Bookings, booking => Assert.Equal(loadedEvent.Id, booking.EventId));
        Assert.Contains(loadedEvent.Bookings, booking => booking.Id == booking1.Id);
        Assert.Contains(loadedEvent.Bookings, booking => booking.Id == booking2.Id);
    }

    [Fact]
    public async Task Update_WhenReschedule_UpdatesEventInDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var @event = new Event(
            "Conference",
            "Will be rescheduled",
            new DateTimeOffset(2026, 07, 10, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 07, 10, 11, 0, 0, TimeSpan.Zero),
            100);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);
        var newStartAt = new DateTimeOffset(2026, 07, 11, 10, 0, 0, TimeSpan.Zero);
        var newEndAt = new DateTimeOffset(2026, 07, 11, 13, 0, 0, TimeSpan.Zero);

        @event.Reschedule(newStartAt, newEndAt);

        // Act
        repository.Update(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == @event.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal(newStartAt, saved.StartAt);
        Assert.Equal(newEndAt, saved.EndAt);
    }

    [Fact]
    public async Task Update_WhenReserveSeats_UpdatesEventInDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var @event = new Event(
            "Workshop",
            "Seats will be reserved",
            new DateTimeOffset(2026, 08, 05, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 08, 05, 16, 0, 0, TimeSpan.Zero),
            10);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);
        var reserved = @event.TryReserveSeats(3);

        // Act
        repository.Update(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == @event.Id, TestContext.Current.CancellationToken);

        Assert.True(reserved);
        Assert.NotNull(saved);
        Assert.Equal(10, saved.TotalSeats);
        Assert.Equal(7, saved.AvailableSeats);
    }

    [Fact]
    public async Task Update_WhenReleaseSeats_UpdatesEventInDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var @event = new Event(
            "Lecture",
            "Seats will be released",
            new DateTimeOffset(2026, 09, 01, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 09, 01, 12, 0, 0, TimeSpan.Zero),
            12);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);
        var reserved = @event.TryReserveSeats(5);
        repository.Update(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        @event.ReleaseSeats(2);

        // Act
        repository.Update(@event);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == @event.Id, TestContext.Current.CancellationToken);

        Assert.True(reserved);
        Assert.NotNull(saved);
        Assert.Equal(12, saved.TotalSeats);
        Assert.Equal(9, saved.AvailableSeats);
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
