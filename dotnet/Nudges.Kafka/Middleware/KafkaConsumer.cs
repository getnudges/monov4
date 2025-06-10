using Confluent.Kafka;

namespace Nudges.Kafka.Middleware;

/// <summary>
/// A Kafka consumer that wraps a <see cref="IConsumer{TKey, TValue}"/> in an <see cref="IAsyncConsumer{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">Key type of Kafka message</typeparam>
/// <typeparam name="TValue">Value type of Kafka message</typeparam>
public sealed class KafkaConsumer<TKey, TValue> : IAsyncConsumer<TKey, TValue>, IAsyncDisposable {
    private readonly IConsumer<TKey, TValue> _consumer;
    private readonly AsyncEnumerator _asyncEnumerator;
    private readonly MessageProcessorOptions _options;

    internal KafkaConsumer(MessageProcessorOptions options) {
        _consumer = new ConsumerBuilder<TKey, TValue>(new ConsumerConfig {
            BootstrapServers = options.BootstrapServers,
            GroupId = $"{options.Topic}-listeners",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = options.AllowAutoCreateTopics,
        })
        .SetKeyDeserializer(new DefaultDeserializer<TKey>())
        .SetValueDeserializer(new DefaultDeserializer<TValue>())
        .Build();
        _asyncEnumerator = new AsyncEnumerator(_consumer, options.StoppingToken ?? CancellationToken.None);
        _options = options;
    }

    /// <summary>
    /// Subscribes to the configured topic.
    /// </summary>
    public Task SubscribeAsync() {
        _consumer.Subscribe(_options.Topic);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Begins consuming messages from the Kafka topic.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>The result of the underlying <see cref="IConsumer{TKey, TValue}"/>'s Consume() call.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<ConsumeResult<TKey, TValue>> ConsumeAsync(CancellationToken cancellationToken) {
        if (await _asyncEnumerator.MoveNextAsync(cancellationToken)) {
            return _asyncEnumerator.Current;
        }
        return null!;
    }

    /// <summary>
    /// Commits the given <see cref="ConsumeResult{TKey, TValue}"/> to the Kafka topic.
    /// </summary>
    /// <param name="result">The <see cref="ConsumeResult{TKey, TValue}"/> to commit.</param>
    public void Commit(ConsumeResult<TKey, TValue> result) => _consumer.Commit(result);

    /// <summary>
    /// Closes the underlying <see cref="IConsumer{TKey, TValue}"/>.
    /// </summary>
    public void Close() => _consumer.Close();

    /// <summary>
    /// Gets an <see cref="IAsyncEnumerator{T}"/> for the <see cref="ConsumeResult{TKey, TValue}"/>s.
    /// </summary>
    public IAsyncEnumerator<ConsumeResult<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => _asyncEnumerator;

    /// <summary>
    /// Disposes the underlying <see cref="IConsumer{TKey, TValue}"/>.
    /// </summary>
    public ValueTask DisposeAsync() => _asyncEnumerator.DisposeAsync();

    /// <summary>
    /// Wraps a <see cref="IConsumer{TKey, TValue}"/> in an <see cref="IAsyncEnumerator{T}"/>.
    /// </summary>
    /// <param name="consumer">The consumer to wrap.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    private class AsyncEnumerator(IConsumer<TKey, TValue> consumer, CancellationToken cancellationToken) : IAsyncEnumerator<ConsumeResult<TKey, TValue>> {

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public ConsumeResult<TKey, TValue> Current { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public ValueTask DisposeAsync() {
            consumer?.Close(); // TODO: look into whether this is ok.  Previsously it's been disposed before I got here.
            consumer?.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() {
            var current = consumer.Consume(cancellationToken);
            if (current is not null) {
                Current = current;
                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }
    }
}
