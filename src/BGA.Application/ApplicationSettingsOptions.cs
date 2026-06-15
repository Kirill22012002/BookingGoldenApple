using System.ComponentModel.DataAnnotations;

namespace BGA.Application;

public class ApplicationSettingsOptions
{
    public const string SectionName = "AppSettings";

    [Range(0, int.MaxValue)]
    public int PoolingIntervalSec { get; set; }

    [Range(0, int.MaxValue)]
    public int ProcessingDelaySec { get; set; }
}
