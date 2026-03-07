using BGA.API.Presentation.Attributes;

namespace BGA.API.Presentation.Dtos;

public record AddEventDto
{
    [FieldRequired]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [FieldRequired(ErrorMessage = $"{nameof(StartAt)} must be filled with valid value (not default value)")]
    public DateTime StartAt { get; set; }

    [FieldRequired(ErrorMessage = $"{nameof(EndAt)} must be filled with valid value (not default value)")]
    [GreaterThan<DateTime>(nameof(StartAt))]
    public DateTime EndAt { get; set; }
}
