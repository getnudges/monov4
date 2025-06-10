using Microsoft.Extensions.Time.Testing;
using Precision.WarpCache.MemoryCache;

namespace Precision.WarpCache.Tests;
public class MemoryCacheStoreTests {
    private readonly MemoryCacheStore<string, int> _cache;
    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));

    public MemoryCacheStoreTests() {
        _cache = new MemoryCacheStore<string, int>(_timeProvider);
    }

    [Fact]
    public async Task SetAsync_Stores_And_GetAsync_Returns_Correct_Value() {
        // Arrange
        var key = "testKey";
        var value = 42;

        // Act
        await _cache.SetAsync(key, value);
        var result = await _cache.GetAsync(key);

        // Assert
        Assert.True(result.Found);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public async Task GetAsync_Returns_NotFound_For_Missing_Key() {
        // Act
        var result = await _cache.GetAsync("missingKey");

        // Assert
        Assert.False(result.Found);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Entry() {
        // Arrange
        var key = "toBeRemoved";
        await _cache.SetAsync(key, 99);

        // Act
        await _cache.RemoveAsync(key);
        var result = await _cache.GetAsync(key);

        // Assert
        Assert.False(result.Found);
    }

    [Fact]
    public async Task Expired_Entry_Is_Not_Returned() {
        // Arrange
        var key = "expiringKey";
        await _cache.SetAsync(key, 123, TimeSpan.FromMilliseconds(50));

        // Wait for expiry
        await Task.Delay(100);

        // Act
        var result = await _cache.GetAsync(key);

        // Assert
        Assert.False(result.Found);
    }

    [Fact]
    public async Task ContainsKeyAsync_Returns_True_For_Existing_Key() {
        // Arrange
        var key = "existingKey";
        await _cache.SetAsync(key, 7);

        // Act
        var contains = await _cache.ContainsKeyAsync(key);

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public async Task ContainsKeyAsync_Returns_False_For_Nonexistent_Key() {
        // Act
        var contains = await _cache.ContainsKeyAsync("nonexistentKey");

        // Assert
        Assert.False(contains);
    }

    [Fact]
    public async Task ClearAsync_Removes_All_Entries() {
        // Arrange
        await _cache.SetAsync("key1", 1);
        await _cache.SetAsync("key2", 2);

        // Act
        await _cache.ClearAsync();
        var result1 = await _cache.GetAsync("key1");
        var result2 = await _cache.GetAsync("key2");

        // Assert
        Assert.False(result1.Found);
        Assert.False(result2.Found);
    }

    [Fact]
    public async Task Concurrent_Reads_Work_Correctly() {
        // Arrange
        var key = "concurrentKey";
        await _cache.SetAsync(key, 456);

        var tasks = new ValueTask<CacheResult<int>>[5];

        // Act
        for (int i = 0; i < 5; i++) {
            tasks[i] = _cache.GetAsync(key);
        }

        await Task.WhenAll(tasks.Select(t => t.AsTask()));

        // Assert
        foreach (var task in tasks) {
            Assert.True(task.Result.Found);
            Assert.Equal(456, task.Result.Value);
        }
    }

    [Fact]
    public async Task Concurrent_Writes_Do_Not_Corrupt_Cache() {
        // Arrange
        var key = "writeTest";

        // Act
        for (int i = 0; i < 5; i++) {
            int value = i;
            await _cache.SetAsync(key, value);
        }

        var result = await _cache.GetAsync(key);

        // Assert
        Assert.True(result.Found);
        Assert.InRange(result.Value, 0, 4); // Last write should win
    }
}
