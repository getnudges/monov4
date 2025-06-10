namespace Nudges.Kafka.Middleware;

public interface IMessageProcessor<TKey, TValue> {
    public Task ProcessMessages(CancellationToken cancellationToken);
}
