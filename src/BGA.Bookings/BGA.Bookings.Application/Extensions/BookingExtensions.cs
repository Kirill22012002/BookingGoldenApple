using BGA.Contracts.Bookings;
using BGA.Bookings.Domain.Models;

namespace BGA.Bookings.Application.Extensions;

public static class BookingExtensions
{
    private const int SeatsCountPerBooking = 1;

    public static BookingConfirmed MapToBookingConfirmed(this Booking booking)
    {
        var confirmedAt = booking.ProcessedAt
            ?? throw new InvalidOperationException("Confirmed booking must have a processed timestamp.");

        return new BookingConfirmed(
            booking.Id,
            booking.EventId,
            booking.UserId,
            SeatsCountPerBooking,
            confirmedAt);
    }
}
