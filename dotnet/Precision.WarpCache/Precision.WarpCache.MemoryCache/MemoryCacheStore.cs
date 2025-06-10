using Microsoft.Extensions.Caching.Memory;

namespace Precision.WarpCache.MemoryCache;
public sealed class MemoryCacheStore<TKey, TValue> : ICacheStore<TKey, TValue>, IDisposable where TKey : notnull {
    private readonly Microsoft.Extensions.Caching.Memory.MemoryCache _cache;
    private readonly MemoryCacheEntryOptions _defaultOptions;
    private readonly TimeProvider _timeProvider;

    private record CacheEntry<T>(T Value, DateTimeOffset? Expiry);

    public MemoryCacheStore(TimeProvider timeProvider) {
        _cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        _defaultOptions = new MemoryCacheEntryOptions {
            SlidingExpiration = TimeSpan.FromMinutes(10) // Default expiration
        };
        _timeProvider = timeProvider;
    }

    public ValueTask<CacheResult<TValue>> GetAsync(TKey key, CancellationToken cancellationToken = default) {
        if (_cache.TryGetValue(key, out CacheEntry<TValue>? entry)) {
            return new ValueTask<CacheResult<TValue>>(new CacheResult<TValue>(entry!.Value, true, entry.Expiry?.ToUnixTimeSeconds()));
        }
        return new ValueTask<CacheResult<TValue>>(new CacheResult<TValue>(default!, false, null));
    }

    public ValueTask SetAsync(TKey key, TValue value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        var expiryTime = expiry.HasValue ? _timeProvider.GetUtcNow() + expiry.Value : (DateTimeOffset?)null;
        var entry = new CacheEntry<TValue>(value, expiryTime);

        var options = expiry.HasValue
            ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry }
            : _defaultOptions;

        _cache.Set(key, entry, options);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default) {
        _cache.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default) =>
        new(_cache.TryGetValue(key, out _));

    public ValueTask ClearAsync(CancellationToken cancellationToken = default) {
        _cache?.Compact(1.0); // Clears all cache entries
        return ValueTask.CompletedTask;
    }

    public void Dispose() => _cache.Dispose();
}
