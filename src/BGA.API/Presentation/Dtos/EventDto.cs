namespace BGA.API.Presentation.Dtos;

public record EventDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime StartAt { get; set; }
    public required DateTime EndAt { get; set; }
}
