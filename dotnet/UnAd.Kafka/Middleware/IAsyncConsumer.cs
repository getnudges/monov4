using Confluent.Kafka;

namespace UnAd.Kafka.Middleware;

public interface IAsyncConsumer<TKey, TValue> : IAsyncEnumerable<ConsumeResult<TKey, TValue>> {
    public Task SubscribeAsync();
    public Task<ConsumeResult<TKey, TValue>> ConsumeAsync(CancellationToken cancellationToken);
    public void Commit(ConsumeResult<TKey, TValue> result);
    public void Close();
}
