namespace BGA.API.Presentation.Dtos;

public record BookingDto
{
    public required Guid Id { get; set; }
    public required Guid EventId { get; set; }
    public required string Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
