using BGA.Bookings.Domain.Models;
using BGA.Bookings.Domain.Models.Enums;
using BGA.Bookings.Infrastructure.DataAccess.Repositories;
using BGA.Bookings.Infrastructure.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BGA.Bookings.Infrastructure.IntegrationTests;

public class BookingRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetByIdAsync_WhenBookingExists_ReturnsSingleBooking()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var booking = CreateBooking();
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());
        var result = await repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.Status, result.Status);
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
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var booking1 = new Booking(eventId, userId, BookingStatus.Pending, new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));
        var booking2 = new Booking(eventId, userId, BookingStatus.Pending, new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));
        var booking3 = new Booking(eventId, userId, BookingStatus.Pending, new DateTimeOffset(2026, 5, 17, 14, 12, 12, TimeSpan.Zero));
        var booking4 = new Booking(eventId, userId, BookingStatus.Rejected, new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var booking5 = new Booking(eventId, userId, BookingStatus.Confirmed, new DateTimeOffset(2026, 9, 14, 12, 0, 0, TimeSpan.Zero));
        await context.Bookings.AddRangeAsync([booking1, booking2, booking3, booking4, booking5], TestContext.Current.CancellationToken);
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
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var activeBooking = new Booking(eventId, userId, BookingStatus.Pending, DateTimeOffset.UtcNow);
        var confirmedBooking = new Booking(eventId, userId, BookingStatus.Confirmed, DateTimeOffset.UtcNow);
        var rejectedBooking = new Booking(eventId, userId, BookingStatus.Rejected, DateTimeOffset.UtcNow);
        var cancelledBooking = new Booking(eventId, userId, BookingStatus.Pending, DateTimeOffset.UtcNow);
        cancelledBooking.Cancel();
        var anotherUsersBooking = new Booking(eventId, anotherUserId, BookingStatus.Pending, DateTimeOffset.UtcNow);
        await context.Bookings.AddRangeAsync([activeBooking, confirmedBooking, rejectedBooking, cancelledBooking, anotherUsersBooking], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());
        var result = await repository.CountActiveByUserIdAsync(userId, TestContext.Current.CancellationToken);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CreateAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        var booking = CreateBooking();

        await repository.CreateAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.SingleAsync(entity => entity.Id == booking.Id, TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Pending, saved.Status);
        Assert.Equal(booking.CreatedAt, saved.CreatedAt);
    }

    [Fact]
    public async Task Update_WhenUpdateStatusToConfirm_UpdatesBookingInDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var booking = CreateBooking();
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Confirm();
        new BookingRepository(context).Update(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.SingleAsync(entity => entity.Id == booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
    }

    [Fact]
    public async Task Update_WhenUpdateStatusToReject_UpdatesBookingInDatabase()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var booking = CreateBooking();
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        booking.Reject();
        new BookingRepository(context).Update(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.SingleAsync(entity => entity.Id == booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Rejected, saved.Status);
    }

    private static Booking CreateBooking()
    {
        return new Booking(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BookingStatus.Pending,
            new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));
    }
}
