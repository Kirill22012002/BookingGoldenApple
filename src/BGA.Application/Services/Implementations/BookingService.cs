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
    private const int MaxActiveBookingsPerUser = 10;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User not found");
        var @event = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken) ?? throw new NotFoundException("Event not found");
        var now = _timeProvider.GetUtcNow();

        if (@event.StartAt <= now)
            throw new EventAlreadyStartedException("Cannot book an event that has already started.");

        var activeBookingsCount = await _unitOfWork.Bookings.CountActiveByUserIdAsync(user.Id, cancellationToken);
        if (activeBookingsCount >= MaxActiveBookingsPerUser)
            throw new BookingLimitExceededException($"User cannot have more than {MaxActiveBookingsPerUser} active bookings.");

        var successReservation = @event.TryReserveSeats();
        if (!successReservation)
            throw new NoAvailableSeatsException("No available seats for this event");

        _unitOfWork.Events.Update(@event);

        var booking = new Booking(eventId, userId, BookingStatus.Pending, _timeProvider.GetUtcNow());

        await _unitOfWork.Bookings.CreateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole userRole, CancellationToken cancellationToken = default)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken) ?? throw new NotFoundException("Booking not found");

        if (userRole != UserRole.Admin && booking.UserId != userId)
            throw new OperationForbiddenException("You do not have permission to cancel this booking.");

        var shouldReleaseSeat = booking.Status is BookingStatus.Pending or BookingStatus.Confirmed;
        booking.Cancel();
        _unitOfWork.Bookings.Update(booking);

        if (shouldReleaseSeat)
        {
            var @event = await _unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken);
            if (@event is not null)
            {
                @event.ReleaseSeats();
                _unitOfWork.Events.Update(@event);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
