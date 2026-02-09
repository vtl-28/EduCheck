using System.Drawing;
using EduCheck.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace EduCheck.Tests.Services;

public class MemoryCacheServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<MemoryCacheService>> _loggerMock;
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<MemoryCacheService>>();
        _cacheService = new MemoryCacheService(_memoryCache, _loggerMock.Object);
    }

    public class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsValue()
    {

        var key = "test_key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };
        await _cacheService.SetAsync(key, value);


        var result = await _cacheService.GetAsync<TestCacheObject>(key);


        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_NonExistingKey_ReturnsNull()
    {

        var result = await _cacheService.GetAsync<TestCacheObject>("non_existing_key");

        result.Should().BeNull();
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_ValidValue_StoresInCache()
    {

        var key = "test_key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };


        await _cacheService.SetAsync(key, value);


        var result = await _cacheService.GetAsync<TestCacheObject>(key);
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_RespectsExpiration()
    {

        var key = "expiring_key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMilliseconds(100);


        await _cacheService.SetAsync(key, value, expiration);


        var immediateResult = await _cacheService.GetAsync<TestCacheObject>(key);
        immediateResult.Should().NotBeNull();

        await Task.Delay(150);

        var expiredResult = await _cacheService.GetAsync<TestCacheObject>(key);
        expiredResult.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {

        var key = "test_key";
        var originalValue = new TestCacheObject { Id = 1, Name = "Original" };
        var newValue = new TestCacheObject { Id = 2, Name = "New" };

        await _cacheService.SetAsync(key, originalValue);
        await _cacheService.SetAsync(key, newValue);


        var result = await _cacheService.GetAsync<TestCacheObject>(key);
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("New");
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesFromCache()
    {

        var key = "test_key";
        var value = new TestCacheObject { Id = 1, Name = "Test" };
        await _cacheService.SetAsync(key, value);


        await _cacheService.RemoveAsync(key);


        var result = await _cacheService.GetAsync<TestCacheObject>(key);
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_NonExistingKey_DoesNotThrow()
    {
        var act = async () => await _cacheService.RemoveAsync("non_existing_key");

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RemoveByPrefixAsync Tests

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesAllMatchingKeys()
    {

        await _cacheService.SetAsync("user_1_profile", new TestCacheObject { Id = 1, Name = "User 1" });
        await _cacheService.SetAsync("user_1_settings", new TestCacheObject { Id = 2, Name = "Settings 1" });
        await _cacheService.SetAsync("user_2_profile", new TestCacheObject { Id = 3, Name = "User 2" });


        await _cacheService.RemoveByPrefixAsync("user_1_");


        var user1Profile = await _cacheService.GetAsync<TestCacheObject>("user_1_profile");
        var user1Settings = await _cacheService.GetAsync<TestCacheObject>("user_1_settings");
        var user2Profile = await _cacheService.GetAsync<TestCacheObject>("user_2_profile");

        user1Profile.Should().BeNull();
        user1Settings.Should().BeNull();
        user2Profile.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_NoMatchingKeys_DoesNotThrow()
    {

        var act = async () => await _cacheService.RemoveByPrefixAsync("non_existing_prefix_");

        await act.Should().NotThrowAsync();
    }

    #endregion
}
