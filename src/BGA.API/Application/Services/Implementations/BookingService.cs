using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class BookingService(
    IBookingRepository _bookingRepository,
    IEventRepository _eventRepository,
    ILogger<BookingService> _logger,
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

    public async Task<ServiceResponse> ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var eventExists = await _eventRepository.ExistsAsync(booking.EventId, cancellationToken);
            if (eventExists)
            {
                booking.Confirm();
            }
            else
            {
                booking.Reject();
                _logger.LogWarning("Error while processing booking because Event not exist. EventId {EventId}, BookingId {}", booking.EventId, booking.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while processing booking. BookingId {BookingId}", booking.Id);
            List<string> errors = [ex.Message];

            try
            {
                booking.Reject();
                var @event = await _eventRepository.GetByIdAsync(booking.EventId, cancellationToken);
                if (@event != null)
                {
                    @event.ReleaseSeats();
                    await _eventRepository.UpdateAsync(@event, cancellationToken);
                }
            }
            catch (Exception innerException)
            {
                errors.Add(innerException.Message);
                _logger.LogError(innerException,
                    "Compensation failed for booking BookingId {BookingId} during error handling",
                    booking.Id);
            }

            return ServiceResponse.Failure(ex, errors);
        }
        finally
        {
            await _bookingRepository.UpdateAsync(booking, cancellationToken);
            _semaphore.Release();
        }

        return ServiceResponse.Success();
    }
}
