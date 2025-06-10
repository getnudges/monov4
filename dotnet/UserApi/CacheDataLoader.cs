using Precision.WarpCache.Grpc.Client; // Assuming CacheClient is in this namespace

namespace UserApi;

public class CacheDataLoader<TValue>(CacheClient<TValue> cacheClient,
                                           IBatchScheduler batchScheduler,
                                           DataLoaderOptions dataLoaderOptions) : BatchDataLoader<string, TValue?>(batchScheduler, dataLoaderOptions) {

    protected override async Task<IReadOnlyDictionary<string, TValue?>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken) {
        var resultDict = new Dictionary<string, TValue?>();

        // Query all keys from cache in parallel
        var tasks = new List<Task>();
        foreach (var key in keys) {
            tasks.Add(Task.Run(async () => {
                var value = await cacheClient.GetAsync(key);
                lock (resultDict) {
                    resultDict[key] = value is TValue typedValue ? typedValue : default;
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
        return resultDict;
    }
}
