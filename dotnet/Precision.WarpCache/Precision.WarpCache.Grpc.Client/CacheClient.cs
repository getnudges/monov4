using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Grpc.Net.Client;

namespace Precision.WarpCache.Grpc.Client;

public class CacheClient<TValue>(GrpcChannel channel, JsonTypeInfo<TValue> jsonTypeInfo) : ICacheClient<TValue> {
    private readonly CacheGrpcService.CacheGrpcServiceClient _client = new(channel);

    public async Task<TValue?> GetAsync(string key, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(key);

        var request = new CacheRequest { Key = key!.ToString() };
        var response = await _client.GetAsync(request, cancellationToken: cancellationToken);

        if (!response.Success) {
            return default;
        }

        using var byteStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Value));
        return await JsonSerializer.DeserializeAsync(byteStream, jsonTypeInfo, cancellationToken);
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(key);

        var request = new CacheRequest { Key = key!.ToString() };
        var response = await _client.RemoveAsync(request, cancellationToken: cancellationToken);
        return response.Success;
    }

    public async Task<bool> SetAsync(string key, TValue value, TimeSpan? expiration = default, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        const long oneYear = 365 * 24 * 60 * 60;
        var request = new CacheRequest {
            Key = key,
            Value = JsonSerializer.Serialize(value, jsonTypeInfo),
            Expiration = expiration.HasValue ? (long)expiration.Value.TotalSeconds : oneYear,
        };
        var response = await _client.SetAsync(request, cancellationToken: cancellationToken);
        return response.Success;
    }
}
