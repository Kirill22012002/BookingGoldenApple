using BGA.API.Infrastructure.Models.Enums;

namespace BGA.API.Infrastructure.Models;

public class Booking
{
    public Guid Id { get; set; }
    public required Guid EventId { get; set; }
    public required BookingStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
