using BGA.API.Infrastructure.DataAccess.Repositories.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.IntegrationTests.Infrastructure;
using BGA.API.Infrastructure.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.IntegrationTests;

public class BookingRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetByIdAsync_()
    {
    }

    [Fact]
    public async Task GetAllInPendingAsync_()
    {

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
        var createdAt = DateTimeOffset.UtcNow;
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
