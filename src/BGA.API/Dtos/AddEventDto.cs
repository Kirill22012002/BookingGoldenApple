using BGA.API.Attributes;

namespace BGA.API.Dtos;

public record AddEventDto
{
    [FieldRequired]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [FieldRequired]
    public required DateTime StartAt { get; set; }

    [FieldRequired]
    [GreaterThan<DateTime>(nameof(StartAt))]
    public required DateTime EndAt { get; set; }
}
