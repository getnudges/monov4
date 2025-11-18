using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace Nudges.Kafka.Middleware;

public record MessageContext<TKey, TValue>(ConsumeResult<TKey, TValue> ConsumeResult, CancellationToken CancellationToken, int AttemptCount = 0, bool Failed = false);

public delegate Task<MessageContext<TKey, TValue>> MessageHandler<TKey, TValue>(MessageContext<TKey, TValue> context);

public sealed class MessageProcessor<TKey, TValue>(IAsyncConsumer<TKey, TValue> consumer,
                                                   IEnumerable<IMessageMiddleware<TKey, TValue>> middlewares) : IMessageProcessor<TKey, TValue>, IAsyncDisposable {

    private async IAsyncEnumerable<ConsumeResult<TKey, TValue>> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken) {
        await consumer.SubscribeAsync();

        await foreach (var result in consumer.WithCancellation(cancellationToken)) {
            yield return result;
        }
    }

    public async Task ProcessMessages(CancellationToken cancellationToken) {
        await foreach (var cr in ReadMessagesAsync(cancellationToken)) {
            var result = await ProcessMessageAsync(new MessageContext<TKey, TValue>(cr, cancellationToken));
            if (!result.Failed) { // TODO: revisit this.  We only want to commit after success or after all retries are attempted
                consumer.Commit(cr);
            }
        }
    }

    private async Task<MessageContext<TKey, TValue>> ProcessMessageAsync(MessageContext<TKey, TValue> context) {
        MessageHandler<TKey, TValue> next = (c) => Task.FromResult(c);

        foreach (var middleware in middlewares.Reverse()) {
            var nextCopy = next;
            next = async (ctx) => await middleware.InvokeAsync(ctx, nextCopy);
        }
        return await next(context with {
            AttemptCount = context.AttemptCount + 1
        });
    }

    public void Dispose() => consumer.Close();

    public ValueTask DisposeAsync() {
        if (consumer is IAsyncDisposable asyncDisposable) {
            return asyncDisposable.DisposeAsync();
        }
        return ValueTask.CompletedTask;
    }
}
