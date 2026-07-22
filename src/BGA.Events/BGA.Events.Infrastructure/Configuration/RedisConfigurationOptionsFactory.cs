using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace BGA.Events.Infrastructure.Configuration;

public static class RedisConfigurationOptionsFactory
{
    public static ConfigurationOptions Create(IConfiguration configuration)
    {
        var redisSection = configuration.GetRequiredSection("Redis");
        var redisConnectionString = redisSection["ConnectionString"]
            ?? throw new InvalidOperationException("Redis connection string not found.");

        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.ConnectTimeout = TryParseInt(redisSection["ConnectTimeout"], 3000);
        redisOptions.AbortOnConnectFail = TryParseBool(redisSection["AbortOnConnectFail"], false);

        return redisOptions;
    }

    private static int TryParseInt(string? value, int fallback)
    {
        return int.TryParse(value, out var parsedValue) ? parsedValue : fallback;
    }

    private static bool TryParseBool(string? value, bool fallback)
    {
        return bool.TryParse(value, out var parsedValue) ? parsedValue : fallback;
    }
}
