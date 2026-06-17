using BGA.Application.UnitTests.Helpers;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;

namespace BGA.Application.UnitTests;

public class BookingTests
{
    [Fact]
    public void Confirm_ShouldSetBookingStatusToConfirmed_And_SetProcessedAtUtcNow()
    {
        // Arrange
        var expectedStatus = BookingStatus.Confirmed;
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);

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
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(expectedStatus, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);
    }
}
