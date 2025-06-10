using Microsoft.Extensions.Time.Testing;
using Moq;
using Precision.WarpCache.MemoryCache;

namespace Precision.WarpCache.Tests;

public class ChannelCacheMediatorTests {
    private readonly Mock<ICacheStore<string, string>> _mockCacheStore;
    private readonly Mock<IEvictionPolicy<string>> _mockEvictionPolicy;
    private readonly ChannelCacheMediator<string, string> _cacheMediator;
    private readonly FakeTimeProvider _fakeTimeProvider = new(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));

    public ChannelCacheMediatorTests() {
        _mockCacheStore = new Mock<ICacheStore<string, string>>();
        _mockEvictionPolicy = new Mock<IEvictionPolicy<string>>();

        _cacheMediator = new ChannelCacheMediator<string, string>(
            _mockCacheStore.Object,
            _mockEvictionPolicy.Object,
            _fakeTimeProvider,
            capacity: 2 // Small capacity to trigger eviction tests
        );
    }

    [Fact]
    public async Task GetAsync_Returns_CachedValue_When_Exists() {

        // Arrange
        var key = "test-key";
        var expectedValue = "cached-value";
        _mockCacheStore.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new CacheResult<string>(expectedValue, true, null));

        // Act
        var result = await _cacheMediator.GetAsync(key);

        // Assert
        Assert.True(result.Found);
        Assert.Equal(expectedValue, result.Value);
        _mockEvictionPolicy.Verify(x => x.OnAccess(key), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Returns_NotFound_When_Key_Does_Not_Exist() {
        // Arrange
        var key = "missing-key";
        _mockCacheStore.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new CacheResult<string>(default, false, null));

        // Act
        var result = await _cacheMediator.GetAsync(key);

        // Assert
        Assert.False(result.Found);
    }

    [Fact]
    public async Task SetAsync_Stores_Value_In_Cache() {
        // Arrange
        var key = "new-key";
        var value = "new-value";

        _mockCacheStore.Setup(x => x.SetAsync(key, value, null, It.IsAny<CancellationToken>()))
                       .Returns(ValueTask.CompletedTask);

        // Act
        await _cacheMediator.SetAsync(key, value);

        // Assert
        _mockCacheStore.Verify(x => x.SetAsync(key, value, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockEvictionPolicy.Verify(x => x.OnInsert(key), Times.Once);
    }

    [Fact]
    public async Task EvictionPolicy_Removes_Oldest_Item_When_Capacity_Exceeded() {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var key3 = "key3"; // This should trigger eviction

        _mockEvictionPolicy.SetupSequence(x => x.SelectVictim())
            .Returns((string?)null) // First insert: No eviction
            .Returns((string?)null) // Second insert: No eviction
            .Returns(key1);         // Third insert: Evict key1

        _mockCacheStore.Setup(x => x.RemoveAsync(key1, It.IsAny<CancellationToken>()))
                       .Returns(ValueTask.CompletedTask);

        // Act
        await _cacheMediator.SetAsync(key1, "value1");
        await _cacheMediator.SetAsync(key2, "value2");
        await _cacheMediator.SetAsync(key3, "value3"); // Triggers eviction of key1

        // Assert
        _mockEvictionPolicy.Verify(x => x.SelectVictim(), Times.Exactly(3)); // Called on every insert
        _mockCacheStore.Verify(x => x.RemoveAsync(key1, It.IsAny<CancellationToken>()), Times.Once); // Eviction happens only once
    }

    [Fact]
    public async Task Concurrent_Reads_Work_Correctly() {
        // Arrange
        var key = "test-key";
        var expectedValue = "cached-value";

        _mockCacheStore.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new CacheResult<string>(expectedValue, true, null));

        var tasks = new ValueTask<CacheResult<string>>[5];

        // Act
        for (int i = 0; i < 5; i++) {
            tasks[i] = _cacheMediator.GetAsync(key);
        }

        await Task.WhenAll(tasks.Select(t => t.AsTask()));

        // Assert
        foreach (var task in tasks) {
            Assert.True(task.Result.Found);
            Assert.Equal(expectedValue, task.Result.Value);
        }

        _mockEvictionPolicy.Verify(x => x.OnAccess(key), Times.Exactly(5));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenExpired() {
        var key = "test-key";
        var expectedValue = "cached-value";

        _mockCacheStore.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new CacheResult<string>(expectedValue, true, _fakeTimeProvider.GetUtcNow().ToUnixTimeSeconds()));

        await _cacheMediator.SetAsync(key, expectedValue, TimeSpan.FromSeconds(5)); // Expires at 1_700_000_005
        _fakeTimeProvider.Advance(TimeSpan.FromSeconds(10)); // Move past expiration
        var result = await _cacheMediator.GetAsync(key);

        Assert.False(result.Found); // Should be expired
        Assert.Null(result.Value);
    }

}
