using BGA.API.Infrastructure.DataAccess.Repositories.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.IntegrationTests;

public class BookingRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetByIdAsync_()
    {
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

        var booking1 = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 09, 10, 12, 0, 0, TimeSpan.FromHours(0)));
        var booking2 = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.FromHours(0)));
        var booking3 = new Booking(@event.Id, BookingStatus.Pending, new DateTimeOffset(2026, 05, 17, 14, 12, 12, TimeSpan.FromHours(0)));
        var booking4 = new Booking(@event.Id, BookingStatus.Rejected, new DateTimeOffset(2026, 08, 14, 12, 0, 0, TimeSpan.FromHours(0)));
        var booking5 = new Booking(@event.Id, BookingStatus.Confirmed, new DateTimeOffset(2026, 09, 14, 12, 0, 0, TimeSpan.FromHours(0)));
        await context.AddRangeAsync(booking1, booking2, booking3, booking4, booking5);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(CreateContext());

        // Act
        var result = await repository.GetAllInPendingAsync(TestContext.Current.CancellationToken);

        // Arrange
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Contains(BookingStatus.Pending, result.Select(b => b.Status));
        Assert.Equal(new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.FromHours(0)), result.First().CreatedAt);
        Assert.Equal(new DateTimeOffset(2026, 09, 10, 12, 0, 0, TimeSpan.FromHours(0)), result.Last().CreatedAt);
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
        var createdAt = new DateTimeOffset(2026, 03, 14, 12, 0, 0, TimeSpan.FromHours(0));
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
    public void Update_()
    {

    }
}
