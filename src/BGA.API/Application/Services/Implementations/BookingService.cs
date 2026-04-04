using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class BookingService(IBookingRepository _bookingRepository) : IBookingService
{
    public async Task<ServiceResponse<Booking>> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = new Booking
            {
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var success = await _bookingRepository.CreateAsync(booking, cancellationToken);

            return success
                ? ServiceResponse<Booking>.Success(booking)
                : ServiceResponse<Booking>.Failure(["Cannot create booking"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse<Booking>.Failure([ex.Message]);
        }
    }

    public async Task<ServiceResponse<Booking>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            return booking != null
                ? ServiceResponse<Booking>.Success(booking)
                : ServiceResponse<Booking>.Failure(["Booking not found"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse<Booking>.Failure([ex.Message]);
        }
    }
}
