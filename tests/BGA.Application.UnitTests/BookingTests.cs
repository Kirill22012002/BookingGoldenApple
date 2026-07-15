using BGA.Application.UnitTests.Helpers;
using BGA.Domain.Exceptions;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;

namespace BGA.Application.UnitTests;

public class BookingTests
{
    [Fact]
    public void Confirm_ShouldSetBookingStatusToConfirmed_And_SetProcessedAtUtcNow()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Reject_ShouldSetBookingStatusToRejected_And_SetProcessedAtUtcNow()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Cancel_ShouldSetBookingStatusToCancelled()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Cancel_WhenBookingAlreadyCancelled_ShouldThrowValidationException()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);
        booking.Cancel();

        var exception = Assert.Throws<ValidationException>(booking.Cancel);

        exception.HasSingleError("Status", "Booking is already cancelled.");
    }
}
