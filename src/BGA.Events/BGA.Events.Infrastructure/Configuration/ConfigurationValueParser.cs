namespace BGA.Events.Infrastructure.Configuration;

public static class ConfigurationValueParser
{
    public static int TryParseInt(string? value, int fallback)
    {
        return int.TryParse(value, out var parsedValue) ? parsedValue : fallback;
    }

    public static bool TryParseBool(string? value, bool fallback)
    {
        return bool.TryParse(value, out var parsedValue) ? parsedValue : fallback;
    }
}
