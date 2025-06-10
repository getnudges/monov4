using System.Collections.Concurrent;

namespace Precision.WarpCache;

/// <summary>
/// A high-performance, in-memory cache optimized for struct storage.
/// </summary>
public class StructCache<TKey, TValue> : ICacheStore<TKey, TValue>
    where TKey : notnull
    where TValue : struct {
    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache;
    private readonly int _capacity;
    private readonly ConcurrentQueue<TKey> _evictionQueue;

    private struct CacheEntry {
        public TValue Value;
        public DateTime Expiry;
    }

    public StructCache(int capacity = 1000) {
        _capacity = capacity;
        _cache = new ConcurrentDictionary<TKey, CacheEntry>();
        _evictionQueue = new ConcurrentQueue<TKey>();
    }

    public ValueTask<CacheResult<TValue>> GetAsync(TKey key, CancellationToken cancellationToken = default) {
        if (_cache.TryGetValue(key, out var entry)) {
            if (entry.Expiry < DateTime.UtcNow) // ✅ Expired? Remove and return not found.
            {
                _cache.TryRemove(key, out _);
                return new ValueTask<CacheResult<TValue>>(new CacheResult<TValue>(default, false, null));
            }

            return new ValueTask<CacheResult<TValue>>(new CacheResult<TValue>(entry.Value, true, null));
        }

        return new ValueTask<CacheResult<TValue>>(new CacheResult<TValue>(default, false, null));
    }

    public ValueTask SetAsync(TKey key, TValue value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        var entry = new CacheEntry {
            Value = value,
            Expiry = expiry.HasValue ? DateTime.UtcNow + expiry.Value : DateTime.MaxValue
        };

        _cache[key] = entry;
        _evictionQueue.Enqueue(key);

        if (_cache.Count > _capacity) {
            if (_evictionQueue.TryDequeue(out var victim)) {
                _cache.TryRemove(victim, out _);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default) {
        _cache.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default) =>
        new(_cache.ContainsKey(key));

    public ValueTask ClearAsync(CancellationToken cancellationToken = default) {
        _cache.Clear();
        while (_evictionQueue.TryDequeue(out _)) { } // ✅ Efficient queue clearing
        return ValueTask.CompletedTask;
    }
}
