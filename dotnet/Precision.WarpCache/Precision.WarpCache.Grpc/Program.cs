using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nudges.Telemetry;
using Precision.WarpCache;
using Precision.WarpCache.Grpc;
using Precision.WarpCache.MemoryCache;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind(settings);

if (settings.Otlp.Endpoint is string url) {
    builder.Services.AddOpenTelemetryConfiguration<Program>(url, builder.Environment.ApplicationName);
}

builder.Services.AddSingleton(TimeProvider.System);
// Currently using MemoryCacheStore for simplicity. In production, consider using Redis.
builder.Services.AddSingleton<ICacheStore<string, string>, MemoryCacheStore<string, string>>();
builder.Services.AddSingleton<IEvictionPolicy<string>>(new LruEvictionPolicy<string>(1000));
builder.Services.AddSingleton<ChannelCacheMediator<string, string>>();

builder.Services.AddGrpc(o => o.EnableDetailedErrors = true);

var app = builder.Build();

app.MapGrpcService<CacheService>();

app.Run();
