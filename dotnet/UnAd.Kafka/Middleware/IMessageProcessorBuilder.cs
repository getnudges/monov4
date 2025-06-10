namespace UnAd.Kafka.Middleware;

public interface IMessageProcessorBuilder<TKey, TValue> {
    public IMessageProcessorBuilder<TKey, TValue> Use(IMessageMiddleware<TKey, TValue> middleware);
    public MessageProcessorOptions Options { get; }
    public IMessageProcessor<TKey, TValue> Build();
}

/// <summary>
/// Options for configuring a <see cref="MessageProcessor{TKey, TValue}"/>.
/// </summary>
/// <param name="Topic">Message topic to consume messages from</param>
/// <param name="BootstrapServers">URI(s) of servers to connect to</param>
/// <param name="AllowAutoCreateTopics">(See <seealso cref="Confluent.Kafka.ClientConfig.AllowAutoCreateTopics"/>)</param>
/// <param name="StoppingToken">Token used to stop underlying consumer from blocking.</param>
public record class MessageProcessorOptions(string Topic, string BootstrapServers, bool AllowAutoCreateTopics, CancellationToken? StoppingToken = default);
