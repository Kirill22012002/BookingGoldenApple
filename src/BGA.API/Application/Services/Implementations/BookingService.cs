using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Implementations;

public class BookingService : IBookingService
{
    public async Task<ServiceResponse<Booking>> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResponse<Booking>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
