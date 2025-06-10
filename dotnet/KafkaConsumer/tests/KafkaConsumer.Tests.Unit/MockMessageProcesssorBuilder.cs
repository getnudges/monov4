using Confluent.Kafka;
using Nudges.Kafka.Middleware;

namespace Nudges.Kafka.Tests.Unit;

public sealed class MockConsumerWrapper<TKey, TValue>(IEnumerable<ConsumeResult<TKey, TValue>> consumeResults, CancellationToken cancellationToken) : IAsyncConsumer<TKey, TValue>, IAsyncDisposable {
    private readonly Enumerator _asyncEnumerator = new(new Queue<ConsumeResult<TKey, TValue>>(consumeResults), cancellationToken);
    public Task SubscribeAsync() => Task.CompletedTask;

    public Task<ConsumeResult<TKey, TValue>> ConsumeAsync(CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_asyncEnumerator.Current);
    }

    public void Commit(ConsumeResult<TKey, TValue> result) { }

    public void Close() { }

    public sealed class Enumerator(Queue<ConsumeResult<TKey, TValue>> consumeResults, CancellationToken cancellationToken) : IAsyncEnumerator<ConsumeResult<TKey, TValue>> {

        public ConsumeResult<TKey, TValue> Current { private set; get; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask<bool> MoveNextAsync() {
            if (cancellationToken.IsCancellationRequested) {
                return false;
            }
            await Task.Yield();
            if (consumeResults.TryDequeue(out var result)) {
                Current = result;
                return true;
            }
            Current = null;
            return false;
        }
    }

    public IAsyncEnumerator<ConsumeResult<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => _asyncEnumerator;
    public ValueTask DisposeAsync() {
        Close();
        return ValueTask.CompletedTask;
    }
}


internal sealed class MockMessageProcessorBuilder<TKey, TValue> : IMessageProcessorBuilder<TKey, TValue> {

    private readonly IEnumerable<IMessageMiddleware<TKey, TValue>> _middlewares = [];
    private readonly IEnumerable<ConsumeResult<TKey, TValue>> _messages;
    private readonly Func<IEnumerable<ConsumeResult<TKey, TValue>>, CancellationToken, IAsyncConsumer<TKey, TValue>> _consumerFactory
        = (messages, cancellationToken) => new MockConsumerWrapper<TKey, TValue>(messages, cancellationToken);
    public MessageProcessorOptions Options => new("", "", false);

    internal MockMessageProcessorBuilder(IEnumerable<ConsumeResult<TKey, TValue>> messages) => _messages = messages;

    public MockMessageProcessorBuilder(IEnumerable<IMessageMiddleware<TKey, TValue>> middlewares,
                                       IEnumerable<ConsumeResult<TKey, TValue>> messages,
                                       Func<IEnumerable<ConsumeResult<TKey, TValue>>, CancellationToken, IAsyncConsumer<TKey, TValue>> consumerFactory) {
        _middlewares = middlewares;
        _messages = messages;
        _consumerFactory = consumerFactory;
    }

    public MockMessageProcessorBuilder<TKey, TValue> WithConsumerFactory(Func<IEnumerable<ConsumeResult<TKey, TValue>>, CancellationToken, IAsyncConsumer<TKey, TValue>> consumerFactory) =>
       new(_middlewares, _messages, consumerFactory);

    public IMessageProcessorBuilder<TKey, TValue> Use(IMessageMiddleware<TKey, TValue> middleware) =>
        new MockMessageProcessorBuilder<TKey, TValue>(_middlewares.Concat([middleware]), _messages, _consumerFactory);

    public IMessageProcessor<TKey, TValue> Build() => new MessageProcessor<TKey, TValue>(_consumerFactory(_messages, CancellationToken.None), _middlewares);
}

internal static class MockMessageProcessorBuilder {
    public static MockMessageProcessorBuilder<TKey, TValue> For<TKey, TValue>(IEnumerable<ConsumeResult<TKey, TValue>> messages) =>
        new(messages);
}
