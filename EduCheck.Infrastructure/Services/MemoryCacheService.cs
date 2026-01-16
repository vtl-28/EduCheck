using EduCheck.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EduCheck.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;

    // Track cache keys for prefix-based removal
    private static readonly ConcurrentDictionary<string, bool> CacheKeys = new();

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = _cache.Get<T>(key);

            if (value != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
            }

            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            _cache.Set(key, value, options);
            CacheKeys.TryAdd(key, true);

            _logger.LogDebug("Cache set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            CacheKeys.TryRemove(key, out _);

            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        try
        {
            var keysToRemove = CacheKeys.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                CacheKeys.TryRemove(key, out _);
            }

            _logger.LogDebug("Removed {Count} cache entries with prefix: {Prefix}",
                keysToRemove.Count, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys with prefix: {Prefix}", prefix);
        }

        return Task.CompletedTask;
    }
}