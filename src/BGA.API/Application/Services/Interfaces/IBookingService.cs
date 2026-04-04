using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Interfaces;

public interface IBookingService
{
    Task<ServiceResponse<Booking>> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<Booking>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
