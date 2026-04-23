using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
