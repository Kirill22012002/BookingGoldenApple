namespace BGA.API.Presentation.Dtos;

public record BookingDto
{
    public required Guid Id { get; set; }
    public required Guid EventId { get; set; }
    public required string Status { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
