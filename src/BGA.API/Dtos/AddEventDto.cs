using System.ComponentModel.DataAnnotations;
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
    [DateTimeGreaterThan(nameof(StartAt))]
    public required DateTime EndAt { get; set; }
}
