using BGA.API.Infrastructure.Models.Enums;

namespace BGA.API.Infrastructure.Models;

public class Booking
{
    public Guid Id { get; private set; }
    public BookingStatus Status { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; } = default!;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public Guid EventId { get; private set; } = default!;
    public Event Event { get; private set; } = null!;

    private Booking() { }

    public Booking(Guid eventId, BookingStatus status, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
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
}
