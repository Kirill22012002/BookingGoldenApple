using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
using BGA.Infrastructure.DataAccess.Repositories;
using BGA.Infrastructure.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BGA.Infrastructure.IntegrationTests;

public class BookingRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetByIdAsync_WhenBookingExists_ReturnsSingleBooking()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 4);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createdAt = new DateTimeOffset(2026, 09, 10, 12, 0, 0, TimeSpan.Zero);
        var booking = new Booking(@event.Id, BookingStatus.Pending, createdAt);
        await context.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());

        // Act
        var result = await repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingNotExists_ReturnsNull()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var repository = new BookingRepository(CreateContext());
        var notExistsId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(notExistsId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllInPendingAsync_ReturnsOnlyPendingBookings_OrderedByCreatedAt()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 4);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var booking1 = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 09, 10, 12, 0, 0, TimeSpan.Zero));
        var booking2 = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero));
        var booking3 = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 05, 17, 14, 12, 12, TimeSpan.Zero));
        var booking4 = new Booking(@event.Id, BookingStatus.Rejected, new DateTimeOffset(2026, 08, 14, 12, 0, 0, TimeSpan.Zero));
        var booking5 = new Booking(@event.Id, BookingStatus.Confirmed, new DateTimeOffset(2026, 09, 14, 12, 0, 0, TimeSpan.Zero));
        await context.AddRangeAsync([booking1, booking2, booking3, booking4, booking5], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());

        // Act
        var result = await repository.GetAllInPendingAsync(TestContext.Current.CancellationToken);

        // Arrange
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Contains(BookingStatus.Pending, result.Select(b => b.Status));
        Assert.Equal(new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero), result.First().CreatedAt);
        Assert.Equal(new DateTimeOffset(2026, 09, 10, 12, 0, 0, TimeSpan.Zero), result.Last().CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 2);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(context);
        var createdAt = new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero);
        var booking = new Booking(@event.Id, BookingStatus.Pending, createdAt);

        // Act
        await repository.CreateAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Pending, saved.Status);
        Assert.Equal(createdAt, saved.CreatedAt);
    }

    [Fact]
    public async Task Query_WhenIncludingEventForBooking_LoadsRelatedEvent()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var @event = new Event(
            "Architecture meetup",
            "DDD and clean architecture",
            new DateTimeOffset(2026, 10, 20, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 20, 20, 0, 0, TimeSpan.Zero),
            25);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var booking = new Booking(
            @event.Id,
            BookingStatus.Pending,
            new DateTimeOffset(2026, 10, 01, 12, 0, 0, TimeSpan.Zero));
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await using var verifyContext = CreateContext();
        var loadedBooking = await verifyContext.Bookings
            .Include(b => b.Event)
            .SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(loadedBooking.Event);
        Assert.Equal(loadedBooking.EventId, loadedBooking.Event.Id);
        Assert.Equal("Architecture meetup", loadedBooking.Event.Title);
    }

    [Fact]
    public async Task Update_WhenUpdateStatusToConfirm_UpdatesBookingInDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 2);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(context);
        var booking = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero));
        await context.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Confirm();

        // Act
        repository.Update(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
    }

    [Fact]
    public async Task Update_WhenUpdateStatusToReject_UpdatesBookingInDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 2);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(context);
        var booking = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero));
        await context.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Reject();

        // Act
        repository.Update(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Rejected, saved.Status);
    }
}
