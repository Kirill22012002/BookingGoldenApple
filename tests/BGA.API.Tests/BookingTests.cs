using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Tests.Helpers;

namespace BGA.API.Tests;

public class BookingTests
{
    [Fact]
    public void Confirm_ShouldSetBookingStatusToConfirmed_And_SetProcessedAtUtcNow()
    {
        // Arrange
        var expectedStatus = BookingStatus.Confirmed;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = TestHelper.Yesterday
        };

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(expectedStatus, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);
    }

    [Fact]
    public void Reject_ShouldSetBookingStatusToRejected_And_SetProcessedAtUtcNow()
    {
        // Arrange
        var expectedStatus = BookingStatus.Rejected;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = TestHelper.Yesterday
        };

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(expectedStatus, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);
    }
}
