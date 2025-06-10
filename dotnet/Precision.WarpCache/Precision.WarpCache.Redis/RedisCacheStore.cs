using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Precision.WarpCache.Redis;

public sealed class RedisCacheStore<TValue> : ICacheStore<string, TValue> {
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly JsonSerializerContext _jsonSerializerContext;

    private const string _keyPrefix = nameof(RedisCacheStore<TValue>);

    private record CacheEntry<T>(T Value, DateTimeOffset? Expiry);

    public RedisCacheStore(IConnectionMultiplexer connectionMultiplexer, JsonSerializerContext jsonSerializerContext) {
        _connectionMultiplexer = connectionMultiplexer;
        _jsonSerializerContext = jsonSerializerContext;
    }

    public async ValueTask<CacheResult<TValue>> GetAsync(string key, CancellationToken cancellationToken = default) {
        var db = _connectionMultiplexer.GetDatabase();
        var entry = await db.StringGetAsync($"{_keyPrefix}:{key}");
        if (entry.HasValue) {
            using var byteStream = new MemoryStream(Encoding.UTF8.GetBytes(entry.ToString()));
            var value = await JsonSerializer.DeserializeAsync(byteStream, (JsonTypeInfo<TValue>)_jsonSerializerContext.GetTypeInfo(typeof(TValue))!, cancellationToken);
            return new CacheResult<TValue>(value!, true, null);
        }
        return new CacheResult<TValue>(default!, false, null);
    }

    public async ValueTask SetAsync(string key, TValue value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value, (JsonTypeInfo<TValue>)_jsonSerializerContext.GetTypeInfo(typeof(TValue))!, cancellationToken);
        var db = _connectionMultiplexer.GetDatabase();
        await db.StringSetAsync($"{_keyPrefix}:{key}", Encoding.UTF8.GetString(stream.ToArray()), expiry);
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) {
        var db = _connectionMultiplexer.GetDatabase();
        await db.KeyDeleteAsync($"{_keyPrefix}:{key}");
    }

    public async ValueTask<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default) {
        var db = _connectionMultiplexer.GetDatabase();
        var result = await db.KeyExistsAsync($"{_keyPrefix}:{key}");
        return result;
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default) {
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
        var keys = server.Keys(pattern: $"{_keyPrefix}:*");
        var db = _connectionMultiplexer.GetDatabase();
        foreach (var key in keys) {
            await db.KeyDeleteAsync(key);
        }
    }
}
