using BGA.API.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BGA.API.Dtos;

public record AddEventDto
{
    [FieldRequired]
    public required string Title { get; set; }

    public string? Description { get; set; }

    [FieldRequired(ErrorMessage = $"{nameof(StartAt)} must be filled with valid value (not default value)")]
    public required DateTimeOffset StartAt { get; set; }

    [FieldRequired(ErrorMessage = $"{nameof(EndAt)} must be filled with valid value (not default value)")]
    [GreaterThan<DateTimeOffset>(nameof(StartAt))]
    public required DateTimeOffset EndAt { get; set; }

    [FieldRequired(ErrorMessage = $"{nameof(TotalSeats)} must be filled with valid value (not default value)")]
    [Range(1, int.MaxValue)]
    public required int TotalSeats { get; set; }
}
