namespace BGA.Bookings.API.E2ETests.Models;

public sealed class BookingResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Status { get; set; } = null!;
}

