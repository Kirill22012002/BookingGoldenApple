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

        await using var context = CreateContext();
        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 4);
        var user = CreateUser();
        var booking = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 09, 10, 12, 0, 0, TimeSpan.Zero));
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());
        var result = await repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(booking.CreatedAt, result.CreatedAt);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingNotExists_ReturnsNull()
    {
        await ResetDatabaseAsync();

        var repository = new BookingRepository(CreateContext());
        var result = await repository.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllInPendingAsync_ReturnsOnlyPendingBookings_OrderedByCreatedAt()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 4);
        var user = CreateUser();
        var booking1 = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 09, 10, 12, 0, 0, TimeSpan.Zero));
        var booking2 = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero));
        var booking3 = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 05, 17, 14, 12, 12, TimeSpan.Zero));
        var booking4 = new Booking(@event.Id, user.Id, BookingStatus.Rejected, new DateTimeOffset(2026, 08, 14, 12, 0, 0, TimeSpan.Zero));
        var booking5 = new Booking(@event.Id, user.Id, BookingStatus.Confirmed, new DateTimeOffset(2026, 09, 14, 12, 0, 0, TimeSpan.Zero));
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.AddRangeAsync([booking1, booking2, booking3, booking4, booking5], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());
        var result = (await repository.GetAllInPendingAsync(TestContext.Current.CancellationToken)).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, booking => Assert.Equal(BookingStatus.Pending, booking.Status));
        Assert.Equal(booking2.CreatedAt, result.First().CreatedAt);
        Assert.Equal(booking1.CreatedAt, result.Last().CreatedAt);
    }

    [Fact]
    public async Task CountActiveByUserIdAsync_ReturnsOnlyPendingAndConfirmedBookings()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 6);
        var user = CreateUser();
        var anotherUser = CreateUser();
        var activeBooking = new Booking(@event.Id, user.Id, BookingStatus.Pending, DateTimeOffset.UtcNow);
        var confirmedBooking = new Booking(@event.Id, user.Id, BookingStatus.Confirmed, DateTimeOffset.UtcNow);
        var rejectedBooking = new Booking(@event.Id, user.Id, BookingStatus.Rejected, DateTimeOffset.UtcNow);
        var cancelledBooking = new Booking(@event.Id, user.Id, BookingStatus.Pending, DateTimeOffset.UtcNow);
        cancelledBooking.Cancel();
        var anotherUsersBooking = new Booking(@event.Id, anotherUser.Id, BookingStatus.Pending, DateTimeOffset.UtcNow);
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.Users.AddRangeAsync([user, anotherUser], TestContext.Current.CancellationToken);
        await context.AddRangeAsync([activeBooking, confirmedBooking, rejectedBooking, cancelledBooking, anotherUsersBooking], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());
        var result = await repository.CountActiveByUserIdAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CreateAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 2);
        var user = CreateUser();
        var booking = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero));
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(context);
        await repository.CreateAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Pending, saved.Status);
        Assert.Equal(booking.CreatedAt, saved.CreatedAt);
        Assert.Equal(user.Id, saved.UserId);
    }

    [Fact]
    public async Task Query_WhenIncludingEventForBooking_LoadsRelatedEvent()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = new Event("Architecture meetup", "DDD and clean architecture", new DateTimeOffset(2026, 10, 20, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 20, 20, 0, 0, TimeSpan.Zero), 25);
        var user = CreateUser();
        var booking = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 10, 01, 12, 0, 0, TimeSpan.Zero));
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var loadedBooking = await verifyContext.Bookings.Include(b => b.Event).SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(loadedBooking.Event);
        Assert.Equal("Architecture meetup", loadedBooking.Event.Title);
    }

    [Fact]
    public async Task Update_WhenUpdateStatusToConfirm_UpdatesBookingInDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 2);
        var user = CreateUser();
        var booking = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero));
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Confirm();
        new BookingRepository(context).Update(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
    }

    [Fact]
    public async Task Update_WhenUpdateStatusToReject_UpdatesBookingInDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var @event = new Event("title", "description", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(2), 2);
        var user = CreateUser();
        var booking = new Booking(@event.Id, user.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.Zero));
        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Reject();
        new BookingRepository(context).Update(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.SingleAsync(b => b.Id == booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Rejected, saved.Status);
    }

    private static User CreateUser()
        => new($"user-{Guid.NewGuid():N}", new string('A', 64), UserRole.User);
}
