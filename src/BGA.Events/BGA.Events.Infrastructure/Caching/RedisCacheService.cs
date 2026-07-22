using BGA.Events.Application.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace BGA.Events.Infrastructure.Caching;

public sealed class RedisCacheService(
    IConnectionMultiplexer multiplexer,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _database.StringGetAsync(key);
            if (!cachedValue.HasValue)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedValue.ToString());
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to read cache value by key {CacheKey}", key);
            return default;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Failed to deserialize cache value by key {CacheKey}", key);
            return default;
        }
        catch (NotSupportedException exception)
        {
            logger.LogWarning(exception, "Failed to deserialize cache value by key {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, ttl);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to write cache value by key {CacheKey}", key);
        }
        catch (NotSupportedException exception)
        {
            logger.LogWarning(exception, "Failed to serialize cache value by key {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to delete cache value by key {CacheKey}", key);
        }
    }
}
