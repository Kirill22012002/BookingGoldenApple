using BGA.API.Presentation.Attributes;

namespace BGA.API.Presentation.Dtos;

public record PutEventDto
{
    [FieldRequired]
    public required string Title { get; set; }

    public string? Description { get; set; }

    [FieldRequired(ErrorMessage = $"{nameof(StartAt)} must be filled with valid value (not default value)")]
    public required DateTimeOffset StartAt { get; set; }

    [FieldRequired(ErrorMessage = $"{nameof(EndAt)} must be filled with valid value (not default value)")]
    [GreaterThan<DateTimeOffset>(nameof(StartAt))]
    public required DateTimeOffset EndAt { get; set; }
}
