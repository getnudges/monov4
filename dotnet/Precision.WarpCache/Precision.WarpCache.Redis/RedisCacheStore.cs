using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using StackExchange.Redis;

namespace Precision.WarpCache.Redis;

public sealed class RedisCacheStore<TValue>(IConnectionMultiplexer connectionMultiplexer,
                                            JsonTypeInfo<TValue> jsonTypeInfo)
    : ICacheStore<string, TValue> {

    private const string KeyPrefix = nameof(RedisCacheStore<>);

    private record CacheEntry<T>(T Value, DateTimeOffset? Expiry);

    public async ValueTask<CacheResult<TValue>> GetAsync(string key, CancellationToken cancellationToken = default) {
        var db = connectionMultiplexer.GetDatabase();
        var entry = await db.StringGetAsync($"{KeyPrefix}:{key}");
        if (entry.HasValue) {
            using var byteStream = new MemoryStream(Encoding.UTF8.GetBytes(entry.ToString()));
            var value = await JsonSerializer.DeserializeAsync(byteStream, jsonTypeInfo, cancellationToken);
            return new CacheResult<TValue>(value!, true, null);
        }
        return new CacheResult<TValue>(default!, false, null);
    }

    public async ValueTask SetAsync(string key, TValue value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo, cancellationToken);
        var db = connectionMultiplexer.GetDatabase();
        await db.StringSetAsync($"{KeyPrefix}:{key}", Encoding.UTF8.GetString(stream.ToArray()), expiry);
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) {
        var db = connectionMultiplexer.GetDatabase();
        await db.KeyDeleteAsync($"{KeyPrefix}:{key}");
    }

    public async ValueTask<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default) {
        var db = connectionMultiplexer.GetDatabase();
        var result = await db.KeyExistsAsync($"{KeyPrefix}:{key}");
        return result;
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default) {
        var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints()[0]);
        var keys = server.Keys(pattern: $"{KeyPrefix}:*");
        var db = connectionMultiplexer.GetDatabase();
        foreach (var key in keys) {
            await db.KeyDeleteAsync(key);
        }
    }
}
