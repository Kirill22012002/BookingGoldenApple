using BGA.API.Application.Exceptions;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;

namespace BGA.API.Application.Services.Implementations;

public class BookingService(
    IBookingRepository _bookingRepository,
    IEventRepository _eventRepository,
    ILogger<BookingService> _logger,
    TimeProvider _timeProvider) : IBookingService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken) ?? throw new NotFoundException("Event not found");

        var successReservation = @event.TryReserveSeats();
        if (!successReservation)
            throw new NoAvailableSeatsException("No available seats for this event");

        await _eventRepository.UpdateAsync(@event, cancellationToken);

        var booking = new Booking(eventId, BookingStatus.Pending, _timeProvider.GetUtcNow());

        await _bookingRepository.CreateAsync(booking, cancellationToken);
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        return booking ?? throw new NotFoundException("Booking not found");
    }

    public async Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
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
                _logger.LogError(innerException,
                    "Compensation failed for booking BookingId {BookingId} during error handling",
                    booking.Id);
            }

            throw;
        }
        finally
        {
            await _bookingRepository.UpdateAsync(booking, cancellationToken);
            _semaphore.Release();
        }
    }
}
