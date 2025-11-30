using System.Threading.Channels;

namespace Precision.WarpCache;

public sealed class ChannelCacheMediator<TKey, TValue> : IDisposable where TKey : notnull {
    private readonly ICacheStore<TKey, TValue> _cacheStore;
    private readonly IEvictionPolicy<TKey> _evictionPolicy;
    private readonly TimeProvider _timeProvider;
    private readonly Channel<CacheOperation<TKey, TValue>> _channel;
    private readonly CancellationTokenSource _cts;

    private readonly CacheResult<TValue> _emptyResult = new(default!, false, null);

    public ChannelCacheMediator(
        ICacheStore<TKey, TValue> cacheStore,
        IEvictionPolicy<TKey> evictionPolicy,
        TimeProvider timeProvider,
        int capacity = 1000) {
        _cacheStore = cacheStore;
        _evictionPolicy = evictionPolicy;
        _timeProvider = timeProvider;
        _channel = Channel.CreateBounded<CacheOperation<TKey, TValue>>(
            new BoundedChannelOptions(capacity) {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        _cts = new CancellationTokenSource();
        _ = ProcessOperationsAsync(_cts.Token);
    }

    public async ValueTask<CacheResult<TValue>> GetAsync(TKey key, CancellationToken token = default) {
        var tcs = new TaskCompletionSource<CacheResult<TValue>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = CacheOperationExtensions.CreateGetOperation(key, tcs);
        await _channel.Writer.WriteAsync(operation, token);
        return await tcs.Task;
    }

    public async ValueTask<CacheResult<TValue>> SetAsync(
        TKey key,
        TValue value,
        TimeSpan? expiry = null,
        CancellationToken token = default) {
        var tcs = new TaskCompletionSource<CacheResult<TValue>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = CacheOperationExtensions.CreateSetOperation(key, value, expiry, tcs);
        await _channel.Writer.WriteAsync(operation, token);
        return await tcs.Task;
    }

    public async ValueTask<CacheResult<TValue>> RemoveAsync(TKey key, CancellationToken token = default) {
        var tcs = new TaskCompletionSource<CacheResult<TValue>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = CacheOperationExtensions.CreateRemoveOperation(key, tcs);
        await _channel.Writer.WriteAsync(operation, token);
        return await tcs.Task;
    }

    private async Task ProcessOperationsAsync(CancellationToken token) {
        try {
            while (await _channel.Reader.WaitToReadAsync(token)) {
                while (_channel.Reader.TryRead(out var operation)) {
                    try {
                        switch (operation.OperationType) {
                            case CacheOperationType.Get:
                                var getResult = await _cacheStore.GetAsync(operation.Key, token);
                                if (getResult.Found) {
                                    var now = _timeProvider.GetUtcNow();
                                    if (getResult.ExpiryTime.HasValue && now > now.AddSeconds(getResult.ExpiryTime.Value)) {
                                        await _cacheStore.RemoveAsync(operation.Key, token);
                                        operation.CompletionSource.TrySetResult(_emptyResult);
                                    } else {
                                        _evictionPolicy.OnAccess(operation.Key);
                                        operation.CompletionSource.TrySetResult(getResult);
                                    }
                                } else {
                                    operation.CompletionSource.TrySetResult(_emptyResult);
                                }
                                break;

                            case CacheOperationType.Set:
                                long? expiryUnix = operation.Expiry.HasValue
                                    ? _timeProvider.GetUtcNow().ToUnixTimeSeconds() + (long)operation.Expiry.Value.TotalSeconds
                                    : null;

                                var victim = _evictionPolicy.SelectVictim();
                                if (victim is not null) {
                                    await _cacheStore.RemoveAsync(victim, token);
                                }

                                await _cacheStore.SetAsync(operation.Key, operation.Value, operation.Expiry, token);
                                _evictionPolicy.OnInsert(operation.Key);
                                operation.CompletionSource.TrySetResult(new CacheResult<TValue>(operation.Value, true, expiryUnix));
                                break;
                            case CacheOperationType.Remove:
                                await _cacheStore.RemoveAsync(operation.Key, token);
                                operation.CompletionSource.TrySetResult(_emptyResult);
                                break;
                            case CacheOperationType.Clear:
                                operation.CompletionSource.TrySetResult(_emptyResult);
                                break;
                            case CacheOperationType.ContainsKey:
                                operation.CompletionSource.TrySetResult(_emptyResult);
                                break;
                            default:
                                operation.CompletionSource.TrySetResult(_emptyResult);
                                break;
                        }
                    } catch (Exception ex) {
                        operation.CompletionSource.TrySetException(ex);
                    }
                }
            }
        } catch (OperationCanceledException) when (token.IsCancellationRequested) {
        } catch (Exception) {
            System.Diagnostics.Debugger.Break();
        }
    }

    public void Dispose() => _cts.Dispose();
}

public record struct CacheResult<TValue>(TValue Value, bool Found, long? ExpiryTime);


public interface IEvictionPolicy<TKey> where TKey : notnull {
    public void OnAccess(TKey key);
    public void OnInsert(TKey key);
    public TKey SelectVictim();
}

public class LruEvictionPolicy<TKey>(int capacity) : IEvictionPolicy<TKey> where TKey : notnull {
    private readonly LinkedList<TKey> _accessOrder = new();
    private readonly Dictionary<TKey, LinkedListNode<TKey>> _nodeMap = [];

    public void OnAccess(TKey key) {
        if (_nodeMap.TryGetValue(key, out var node)) {
            _accessOrder.Remove(node);
            _accessOrder.AddLast(node); // Move key to the end (most recently used)
        }
    }

    public void OnInsert(TKey key) {
        if (_nodeMap.ContainsKey(key)) {
            return; // Ignore duplicate insertions
        }

        // Add key to the end (most recently used)
        var node = _accessOrder.AddLast(key);
        _nodeMap[key] = node;

        // Evict if over capacity
        if (_accessOrder.Count > capacity) {
            var victim = SelectVictim();
            if (victim != null) {
                _nodeMap.Remove(victim);
                _accessOrder.RemoveFirst(); // Remove least recently used item
            }
        }
    }

    public TKey SelectVictim() => _accessOrder.Count > 0 ? _accessOrder.First!.Value : default!;
}


public static class CacheOperationExtensions {
    /// <summary>
    /// Creates a new Get operation.
    /// </summary>
    public static CacheOperation<TKey, TValue> CreateGetOperation<TKey, TValue>(
        TKey key,
        TaskCompletionSource<CacheResult<TValue>> completionSource) where TKey : notnull => new(
            CacheOperationType.Get,
            key,
            default!,
            null,
            completionSource);

    /// <summary>
    /// Creates a new Set operation.
    /// </summary>
    public static CacheOperation<TKey, TValue> CreateSetOperation<TKey, TValue>(
        TKey key,
        TValue value,
        TimeSpan? expiry,
        TaskCompletionSource<CacheResult<TValue>> completionSource) where TKey : notnull => new(
            CacheOperationType.Set,
            key,
            value,
            expiry,
            completionSource);

    /// <summary>
    /// Creates a new Remove operation.
    /// </summary>
    public static CacheOperation<TKey, TValue> CreateRemoveOperation<TKey, TValue>(
        TKey key,
        TaskCompletionSource<CacheResult<TValue>> completionSource) where TKey : notnull => new(
            CacheOperationType.Remove,
            key,
            default!,
            null,
            completionSource);
}


public record struct CacheOperation<TKey, TValue>(
    CacheOperationType OperationType,
    TKey Key,
    TValue Value,
    TimeSpan? Expiry,
    TaskCompletionSource<CacheResult<TValue>> CompletionSource) where TKey : notnull;

/// <summary>
/// Enumeration of possible cache operations.
/// </summary>
public enum CacheOperationType {
    Get,
    Set,
    Remove,
    Clear,
    ContainsKey
}
