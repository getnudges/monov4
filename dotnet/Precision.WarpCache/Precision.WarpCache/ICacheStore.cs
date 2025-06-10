namespace Precision.WarpCache;

public interface ICacheStore<TKey, TValue> where TKey : notnull {
    public ValueTask<CacheResult<TValue>> GetAsync(TKey key, CancellationToken cancellationToken = default);
    public ValueTask SetAsync(TKey key, TValue value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    public ValueTask RemoveAsync(TKey key, CancellationToken cancellationToken = default);
    public ValueTask<bool> ContainsKeyAsync(TKey key, CancellationToken cancellationToken = default);
    public ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
