using BGA.Bookings.Application.Repositories;
using BGA.Bookings.Application.Services.Interfaces;
using BGA.Bookings.Domain.Exceptions;
using BGA.Bookings.Domain.Models;
using BGA.Bookings.Domain.Models.Enums;
using Microsoft.Extensions.Logging;

namespace BGA.Bookings.Application.Services.Implementations;

public sealed class BookingService(
    IUnitOfWork unitOfWork,
    ILogger<BookingService> logger,
    TimeProvider timeProvider) : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;

    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var activeBookingsCount = await unitOfWork.Bookings.CountActiveByUserIdAsync(userId, cancellationToken);
        if (activeBookingsCount >= MaxActiveBookingsPerUser)
        {
            throw new BookingLimitExceededException($"User cannot have more than {MaxActiveBookingsPerUser} active bookings.");
        }

        var booking = new Booking(eventId, userId, BookingStatus.Pending, timeProvider.GetUtcNow());
        await unitOfWork.Bookings.CreateAsync(booking, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole userRole, CancellationToken cancellationToken = default)
    {
        var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken) ?? throw new NotFoundException("Booking not found");

        if (userRole != UserRole.Admin && booking.UserId != userId)
        {
            throw new OperationForbiddenException("You do not have permission to cancel this booking.");
        }

        booking.Cancel();
        unitOfWork.Bookings.Update(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken) ?? throw new NotFoundException("Booking not found");
    }

    public async Task ProcessBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        if (booking.Status != BookingStatus.Pending)
        {
            return;
        }

        try
        {
            booking.Confirm();
            unitOfWork.Bookings.Update(booking);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while processing booking {BookingId}", booking.Id);
            throw;
        }
    }
}
