using BGA.Contracts.Bookings;

namespace BGA.Bookings.Application.Messaging;

public interface IBookingConfirmedPublisher
{
    Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}
