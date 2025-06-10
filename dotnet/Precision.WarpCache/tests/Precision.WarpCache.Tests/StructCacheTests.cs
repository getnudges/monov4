namespace Precision.WarpCache.Tests;

public class StructCacheTests {
    private readonly StructCache<string, TestStruct> _cache;

    public StructCacheTests() {
        _cache = new StructCache<string, TestStruct>(capacity: 2); // Small capacity for eviction testing
    }

    private readonly struct TestStruct {
        public readonly int Id;
        public readonly float Value;

        public TestStruct(int id, float value) {
            Id = id;
            Value = value;
        }
    }

    [Fact]
    public async Task SetAsync_Stores_And_GetAsync_Returns_Correct_Value() {
        // Arrange
        var key = "testKey";
        var value = new TestStruct(42, 3.14f);

        // Act
        await _cache.SetAsync(key, value);
        var result = await _cache.GetAsync(key);

        // Assert
        Assert.True(result.Found);
        Assert.Equal(value.Id, result.Value.Id);
        Assert.Equal(value.Value, result.Value.Value);
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
        var value = new TestStruct(99, 1.23f);
        await _cache.SetAsync(key, value);

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
        var value = new TestStruct(10, 2.71f);
        await _cache.SetAsync(key, value, TimeSpan.FromMilliseconds(50));

        // Wait for expiry
        await Task.Delay(100);

        // Act
        var result = await _cache.GetAsync(key);

        // Assert
        Assert.False(result.Found);
    }

    [Fact]
    public async Task Cache_Evicts_Oldest_Entry_When_Capacity_Is_Exceeded() {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var key3 = "key3"; // Should trigger eviction

        await _cache.SetAsync(key1, new TestStruct(1, 1.1f));
        await _cache.SetAsync(key2, new TestStruct(2, 2.2f));
        await _cache.SetAsync(key3, new TestStruct(3, 3.3f)); // This will evict key1

        // Act
        var result1 = await _cache.GetAsync(key1);
        var result2 = await _cache.GetAsync(key2);
        var result3 = await _cache.GetAsync(key3);

        // Assert
        Assert.False(result1.Found); // Evicted
        Assert.True(result2.Found);
        Assert.True(result3.Found);
    }

    [Fact]
    public async Task Concurrent_Reads_Work_Correctly() {
        // Arrange
        var key = "concurrentKey";
        var value = new TestStruct(123, 4.56f);
        await _cache.SetAsync(key, value);

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _cache.GetAsync(key).AsTask()) // Convert to Task
            .ToArray();

        // Act
        await Task.WhenAll(tasks);

        // Assert
        foreach (var task in tasks) {
            var result = await task; // Await the individual task result
            Assert.True(result.Found);
            Assert.Equal(value.Id, result.Value.Id);
            Assert.Equal(value.Value, result.Value.Value);
        }
    }


    [Fact]
    public async Task Concurrent_Writes_Do_Not_Corrupt_Cache() {
        // Arrange
        var key = "writeTest";
        var tasks = new List<ValueTask>();

        // Act
        for (int i = 0; i < 5; i++) {
            var value = new TestStruct(i, i * 1.1f);
            tasks.Add(_cache.SetAsync(key, value));
        }

        await Task.WhenAll(tasks.Select(t => t.AsTask()));
        var result = await _cache.GetAsync(key);

        // Assert
        Assert.True(result.Found);
        Assert.InRange(result.Value.Id, 0, 4); // Last writer should have won
    }

    [Fact]
    public async Task HighConcurrency_DoesNotFail() {
        var cache = new StructCache<string, int>(capacity: 1000);
        var tasks = Enumerable.Range(0, 1000).Select(i =>
            cache.SetAsync($"key{i}", i)
        );

        await Task.WhenAll(tasks.Select(t => t.AsTask()));

        var readTasks = Enumerable.Range(0, 1000).Select(i =>
            cache.GetAsync($"key{i}").AsTask()
        );

        await Task.WhenAll(readTasks);
    }

}
