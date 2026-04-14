using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class BookingService(
    IBookingRepository _bookingRepository,
    IEventRepository _eventRepository,
    TimeProvider _timeProvider) : IBookingService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<ServiceResponse<Booking>> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
                if (@event == null)
                    return ServiceResponse<Booking>.Failure("Event not found", ServiceErrorType.NotFound);

                var successReservation = @event.TryReserveSeats();
                if (!successReservation)
                    return ServiceResponse<Booking>.Failure("No available seats for this event", ServiceErrorType.Conflict);

                await _eventRepository.UpdateAsync(@event, cancellationToken);

                var booking = new Booking
                {
                    EventId = eventId,
                    Status = BookingStatus.Pending,
                    CreatedAt = _timeProvider.GetUtcNow()
                };

                var success = await _bookingRepository.CreateAsync(booking, cancellationToken);

                return success
                    ? ServiceResponse<Booking>.Success(booking)
                    : ServiceResponse<Booking>.Failure("Cannot create booking", ServiceErrorType.InternalProblem);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            return ServiceResponse<Booking>.Failure(ex, ex.Message);
        }
    }

    public async Task<ServiceResponse<Booking>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            return booking != null
                ? ServiceResponse<Booking>.Success(booking)
                : ServiceResponse<Booking>.Failure("Booking not found", ServiceErrorType.NotFound);
        }
        catch (Exception ex)
        {
            return ServiceResponse<Booking>.Failure(ex, ex.Message);
        }
    }
}
