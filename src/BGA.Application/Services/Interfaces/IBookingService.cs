using BGA.Domain.Models;

namespace BGA.Application.Services.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
