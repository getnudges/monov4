using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Precision.WarpCache.MemoryCache;

namespace Precision.WarpCache.Demo;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)] // Ensure consistent test environment
public class ChannelCacheMediatorBenchmark {
    private readonly StructCache<string, int> _structCache = new(1000);
    private readonly MemoryCacheStore<string, int> _memoryCache = new(TimeProvider.System);
    private readonly ChannelCacheMediator<string, int> _structCacheMediator;
    private readonly ChannelCacheMediator<string, int> _memoryCacheMediator;

    public ChannelCacheMediatorBenchmark() {
        _structCacheMediator = new ChannelCacheMediator<string, int>(_structCache, new LruEvictionPolicy<string>(1000), TimeProvider.System);
        _memoryCacheMediator = new ChannelCacheMediator<string, int>(_memoryCache, new LruEvictionPolicy<string>(1000), TimeProvider.System);
    }

    [Benchmark(Baseline = true)]
    public async Task InsertAndRetrieve_StructCache() {
        await _structCache.SetAsync("key", 42);
        var result = await _structCache.GetAsync("key");
    }

    [Benchmark]
    public async Task InsertAndRetrieve_MemoryCache() {
        await _memoryCache.SetAsync("key", 42);
        var result = await _memoryCache.GetAsync("key");
    }

    [Benchmark]
    public async Task InsertAndRetrieve_StructCache_Mediator() {
        await _structCacheMediator.SetAsync("key", 42);
        var result = await _structCacheMediator.GetAsync("key");
    }

    [Benchmark]
    public async Task InsertAndRetrieve_MemoryCache_Mediator() {
        await _memoryCacheMediator.SetAsync("key", 42);
        var result = await _memoryCacheMediator.GetAsync("key");
    }
}
