namespace Precision.WarpCache.Grpc.Client;

public interface ICacheClient<TValue> {
    public Task<TValue?> GetAsync(string key, CancellationToken cancellationToken = default);
    public Task<bool> SetAsync(string key, TValue value, TimeSpan? expiration = default, CancellationToken cancellationToken = default);
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
}
