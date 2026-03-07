using BGA.API.Presentation.Attributes;

namespace BGA.API.Presentation.Dtos;

public record PutEventDto
{
    [FieldRequired]
    public required string? Title { get; set; }

    public string? Description { get; set; }

    [FieldRequired]
    public required DateTime StartAt { get; set; }

    [FieldRequired]
    [GreaterThan<DateTime>(nameof(StartAt))]
    public required DateTime EndAt { get; set; }
}
