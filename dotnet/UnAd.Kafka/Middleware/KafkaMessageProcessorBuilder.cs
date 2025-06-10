namespace UnAd.Kafka.Middleware;

internal sealed class KafkaMessageProcessorBuilder<TKey, TValue> : IMessageProcessorBuilder<TKey, TValue> {

    private readonly IEnumerable<IMessageMiddleware<TKey, TValue>> _middlewares = [];
    public MessageProcessorOptions Options { get; }

    internal KafkaMessageProcessorBuilder(MessageProcessorOptions options) => Options = options;
    private KafkaMessageProcessorBuilder(IEnumerable<IMessageMiddleware<TKey, TValue>> middlewares, MessageProcessorOptions options) {
        _middlewares = middlewares;
        Options = options;
    }

    public IMessageProcessorBuilder<TKey, TValue> Use(IMessageMiddleware<TKey, TValue> middleware) =>
        new KafkaMessageProcessorBuilder<TKey, TValue>(_middlewares.Concat([middleware]), Options);

    public IMessageProcessor<TKey, TValue> Build() => new MessageProcessor<TKey, TValue>(new KafkaConsumer<TKey, TValue>(Options), _middlewares);
}

public static class KafkaMessageProcessorBuilder {

    public static IMessageProcessorBuilder<TKey, TValue> For<TKey, TValue>(string topic, string servers, bool allowAutoCreateTopics = true, CancellationToken? cancellationToken = default) =>
        new KafkaMessageProcessorBuilder<TKey, TValue>(new MessageProcessorOptions(topic, servers, allowAutoCreateTopics, cancellationToken));
}

