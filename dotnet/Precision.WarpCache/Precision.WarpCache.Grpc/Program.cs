using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Precision.WarpCache;
using Precision.WarpCache.Grpc;
using Precision.WarpCache.MemoryCache;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICacheStore<string, string>, MemoryCacheStore<string, string>>();
builder.Services.AddSingleton<IEvictionPolicy<string>>(new LruEvictionPolicy<string>(1000));
builder.Services.AddSingleton<ChannelCacheMediator<string, string>>();

builder.Services.AddGrpc(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());

var app = builder.Build();

app.MapGrpcService<CacheService>();

app.Run();
