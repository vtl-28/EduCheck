using EduCheck.Application.Interfaces;

namespace EduCheck.IntegrationTests.Fixtures;

/// <summary>
/// Cache implementation that never caches anything.
/// Used in integration tests to ensure all requests hit the real database,
/// avoiding cache invalidation timing issues between test operations.
/// </summary>
public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) where T : class =>
        Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class =>
        Task.CompletedTask;

    public Task RemoveAsync(string key) =>
        Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix) =>
        Task.CompletedTask;
}