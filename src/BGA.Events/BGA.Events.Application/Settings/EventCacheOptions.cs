using System.ComponentModel.DataAnnotations;

namespace BGA.Events.Application.Settings;

public sealed class EventCacheOptions
{
    public const string SectionName = "Cache";

    [Range(1, 1440)]
    public int EventByIdTtlMinutes { get; set; } = 5;

    [Range(1, 1440)]
    public int TopEventsTtlMinutes { get; set; } = 10;
}
