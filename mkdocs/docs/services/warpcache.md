# WarpCache

WarpCache is a high-performance, async in-memory caching service with a gRPC interface. It's used throughout Nudges for token storage, session data, and temporary state.

## Architecture

| Component | Purpose |
|-----------|---------|
| `Precision.WarpCache` | Core library with channel-based async operations |
| `Precision.WarpCache.MemoryCache` | In-memory backend using .NET MemoryCache |
| `Precision.WarpCache.Redis` | Distributed Redis backend |
| `Precision.WarpCache.Grpc` | gRPC server exposing cache operations |
| `Precision.WarpCache.Grpc.Client` | Strongly-typed gRPC client |

## gRPC API

```protobuf
service CacheGrpcService {
    rpc Get (CacheRequest) returns (CacheResponse);
    rpc Set (CacheRequest) returns (CacheResponse);
    rpc Remove (CacheRequest) returns (CacheResponse);
}
```

## Client Usage

```csharp
// Register in DI
services.AddWarpCacheClient<MyType>(settings, MyTypeSerializerContext.Default.MyType);

// Use
var value = await cacheClient.GetAsync("key");
await cacheClient.SetAsync("key", value, TimeSpan.FromMinutes(5));
await cacheClient.RemoveAsync("key");
```

## Features

- **Async channel-based architecture** - Lock-free operation queueing
- **LRU eviction** - Automatic eviction when capacity exceeded (default 1000 items)
- **TTL support** - Expiration with Unix timestamp tracking
- **Pluggable backends** - In-memory or Redis

## Configuration

```ini
Kestrel__Endpoints__gRPC__Url=http://*:7777
Kestrel__EndpointDefaults__Protocols=Http2
Otlp__Endpoint=http://otel-collector:4317
```

## Running

```powershell
cd dotnet/Precision.WarpCache.Server
dotnet run
```

Default port: `7777` (gRPC/HTTP2)
