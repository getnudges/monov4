namespace Precision.WarpCache;

public interface IDataLoader<TKey, TValue> {
    public Task<TValue> LoadAsync(TKey key, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TValue>> LoadAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default);
    public void Clear(TKey key);
    public void ClearAll();
}
