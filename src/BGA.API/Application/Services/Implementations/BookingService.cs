using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class BookingService(IBookingRepository _bookingRepository) : IBookingService
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
