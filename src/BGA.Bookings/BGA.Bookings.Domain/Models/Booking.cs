using BGA.Bookings.Domain.Exceptions;
using BGA.Bookings.Domain.Models.Enums;

namespace BGA.Bookings.Domain.Models;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public Guid EventId { get; private set; }

    private Booking()
    {
    }

    public Booking(Guid eventId, Guid userId, BookingStatus status, DateTimeOffset createdAt)
    {
        ValidateId(eventId, nameof(eventId));
        ValidateId(userId, nameof(userId));

        Id = Guid.NewGuid();
        EventId = eventId;
        UserId = userId;
        Status = status;
        CreatedAt = createdAt;
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new ValidationException(nameof(Status), "Booking is already cancelled.");
        }

        if (Status == BookingStatus.Rejected)
        {
            throw new ValidationException(nameof(Status), "Rejected booking cannot be cancelled.");
        }

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException(fieldName, $"{fieldName} must not be empty.");
        }
    }
}
