namespace Nudges.Kafka.Middleware;

internal sealed class KafkaMessageProcessorBuilder<TKey, TValue> : IMessageProcessorBuilder<TKey, TValue> {

    private readonly IEnumerable<IMessageMiddleware<TKey, TValue>> _middlewares = [];
    private readonly DlqHandler<TKey, TValue>? _dlqHandler = null;
    public MessageProcessorOptions Options { get; }

    internal KafkaMessageProcessorBuilder(MessageProcessorOptions options) => Options = options;
    private KafkaMessageProcessorBuilder(IEnumerable<IMessageMiddleware<TKey, TValue>> middlewares, MessageProcessorOptions options, DlqHandler<TKey, TValue>? dlqHandler = null) {
        _middlewares = middlewares;
        Options = options;
        _dlqHandler = dlqHandler;
    }

    public IMessageProcessorBuilder<TKey, TValue> Use(IMessageMiddleware<TKey, TValue> middleware) =>
        new KafkaMessageProcessorBuilder<TKey, TValue>(_middlewares.Concat([middleware]), Options, _dlqHandler);

    public IMessageProcessorBuilder<TKey, TValue> UseDlq(DlqHandler<TKey, TValue> dlqHandler) =>
        new KafkaMessageProcessorBuilder<TKey, TValue>(_middlewares, Options, dlqHandler);

    public IMessageProcessor<TKey, TValue> Build() => new MessageProcessor<TKey, TValue>(new KafkaConsumer<TKey, TValue>(Options), _middlewares, Options.MaxRetryAttempts, _dlqHandler);
}

public static class KafkaMessageProcessorBuilder {

    public static IMessageProcessorBuilder<TKey, TValue> For<TKey, TValue>(string topic, string servers, bool allowAutoCreateTopics = true, CancellationToken? cancellationToken = default) =>
        new KafkaMessageProcessorBuilder<TKey, TValue>(new MessageProcessorOptions(topic, servers, allowAutoCreateTopics, cancellationToken));
}

