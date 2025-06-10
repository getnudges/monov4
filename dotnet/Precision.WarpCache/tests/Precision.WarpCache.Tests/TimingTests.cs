using Microsoft.Extensions.Time.Testing;
using Precision.WarpCache.MemoryCache;

namespace Precision.WarpCache.Tests;

public class TimingTests {

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenExpired() {
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));
        var cacheStore = new MemoryCacheStore<string, string>(fakeTimeProvider);
        var evictionPolicy = new LruEvictionPolicy<string>(100);

        var cache = new ChannelCacheMediator<string, string>(cacheStore, evictionPolicy, fakeTimeProvider);

        await cache.SetAsync("key", "value", TimeSpan.FromSeconds(5)); // Expires at 1_700_000_005
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(10)); // Move past expiration

        var result = await cache.GetAsync("key");

        Assert.False(result.Found); // Should be expired
        Assert.Null(result.Value);
    }

}
