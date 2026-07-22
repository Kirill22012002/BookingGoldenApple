using BGA.Bookings.Domain.Models;
using BGA.Bookings.Domain.Models.Enums;

namespace BGA.Bookings.Application.Services.Interfaces;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole userRole, CancellationToken cancellationToken = default);
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
