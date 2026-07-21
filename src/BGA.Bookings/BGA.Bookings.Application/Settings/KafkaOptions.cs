using System.ComponentModel.DataAnnotations;

namespace BGA.Bookings.Application.Settings;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    [Required]
    public string BootstrapServers { get; set; } = string.Empty;
}
