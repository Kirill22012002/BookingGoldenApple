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
        redisOptions.ConnectTimeout = ConfigurationValueParser.TryParseInt(redisSection["ConnectTimeout"], 3000);
        redisOptions.ConnectRetry = ConfigurationValueParser.TryParseInt(redisSection["ConnectRetry"], 0);
        redisOptions.AbortOnConnectFail = ConfigurationValueParser.TryParseBool(redisSection["AbortOnConnectFail"], false);

        return redisOptions;
    }
}
