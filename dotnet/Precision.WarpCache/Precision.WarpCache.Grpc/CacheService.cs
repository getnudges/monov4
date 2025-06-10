using Grpc.Core;

namespace Precision.WarpCache.Grpc;

public class CacheService(ChannelCacheMediator<string, string> cacheMediator) : CacheGrpcService.CacheGrpcServiceBase {
    public override async Task<CacheResponse> Get(CacheRequest request, ServerCallContext context) {
        var result = await cacheMediator.GetAsync(request.Key);
        return new CacheResponse { Success = !string.IsNullOrEmpty(result.Value), Value = result.Value ?? string.Empty };
    }

    public override async Task<CacheResponse> Set(CacheRequest request, ServerCallContext context) {
        var result = await cacheMediator.SetAsync(
            request.Key,
            request.Value,
            TimeSpan.FromSeconds(request.Expiration));
        return new CacheResponse { Success = result.Value is not null, Value = result.Value ?? string.Empty };
    }

    public override async Task<CacheResponse> Remove(CacheRequest request, ServerCallContext context) {
        var result = await cacheMediator.RemoveAsync(request.Key);
        return new CacheResponse { Success = result.Value is not null, Value = result.Value ?? string.Empty };
    }
}

