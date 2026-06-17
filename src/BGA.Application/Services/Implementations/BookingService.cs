using BGA.Application.Repositories;
using BGA.Application.Services.Interfaces;
using BGA.Domain.Exceptions;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
using Microsoft.Extensions.Logging;

namespace BGA.Application.Services.Implementations;

public class BookingService(
    IUnitOfWork _unitOfWork,
    ILogger<BookingService> _logger,
    TimeProvider _timeProvider) : IBookingService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var @event = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken) ?? throw new NotFoundException("Event not found");

        var successReservation = @event.TryReserveSeats();
        if (!successReservation)
            throw new NoAvailableSeatsException("No available seats for this event");

        _unitOfWork.Events.Update(@event);

        var booking = new Booking(eventId, BookingStatus.Pending, _timeProvider.GetUtcNow());

        await _unitOfWork.Bookings.CreateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken);
        return booking ?? throw new NotFoundException("Booking not found");
    }

    public async Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var eventExists = await _unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken);
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
                var @event = await _unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken);
                if (@event != null)
                {
                    @event.ReleaseSeats();
                    _unitOfWork.Events.Update(@event);
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
            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _semaphore.Release();
        }
    }
}
