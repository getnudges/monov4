using System.Diagnostics;
using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace Nudges.Kafka.Middleware;

public record MessageContext<TKey, TValue>(
    ConsumeResult<TKey, TValue> ConsumeResult,
    CancellationToken CancellationToken,
    int AttemptCount = 0,
    FailureType Failure = FailureType.None);

public enum FailureType {
    None,           // No failure, message processed successfully
    Transient,      // Might succeed on retry (timeouts, network blips, etc.)
    Permanent,      // Will never succeed (validation errors, bad payload)
    DependencyDown, // A downstream system is unavailable (DB/Kafka/Keycloak/etc.)
    Fatal           // Unrecoverable error, do not retry
}


public delegate Task<MessageContext<TKey, TValue>> MessageHandler<TKey, TValue>(MessageContext<TKey, TValue> context);

public sealed class MessageProcessor<TKey, TValue>(
    IAsyncConsumer<TKey, TValue> consumer,
    IEnumerable<IMessageMiddleware<TKey, TValue>> middlewares)
    : IMessageProcessor<TKey, TValue>, IAsyncDisposable {

    // How many times we’re willing to retry before giving up
    private const int MaxRetryAttempts = 5;
    private async IAsyncEnumerable<ConsumeResult<TKey, TValue>> ReadMessagesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken) {

        await consumer.SubscribeAsync();

        await foreach (var result in consumer.WithCancellation(cancellationToken)) {
            yield return result;
        }
    }

    public async Task ProcessMessages(CancellationToken cancellationToken) {
        await foreach (var cr in ReadMessagesAsync(cancellationToken)) {
            var context = new MessageContext<TKey, TValue>(cr, cancellationToken);

            // Pipeline handles ALL retry/circuit-breaking/error handling
            context = await ProcessMessageAsync(context);

            // Just handle the final outcome
            switch (context.Failure) {
                case FailureType.None:
                    consumer.Commit(cr);
                    break;

                case FailureType.Permanent:
                case FailureType.Fatal:
                    RouteToDlq(cr, context);
                    consumer.Commit(cr);
                    break;
                case FailureType.Transient:
                    break;
                case FailureType.DependencyDown:
                    break;

                // Transient/DependencyDown that exhausted retries
                default:
                    if (context.AttemptCount >= MaxRetryAttempts) {
                        RouteToDlq(cr, context);
                        consumer.Commit(cr);
                    }
                    break;
            }
        }
    }

    private async Task<MessageContext<TKey, TValue>> ProcessMessageAsync(MessageContext<TKey, TValue> context) {
        // Build pipeline from the registered middlewares
        MessageHandler<TKey, TValue> next = static c => Task.FromResult(c);

        foreach (var middleware in middlewares.Reverse()) {
            var nextCopy = next;
            next = async ctx => await middleware.InvokeAsync(ctx, nextCopy);
        }

        // NOTE: we do NOT increment AttemptCount here anymore.
        // That’s owned by the outer retry loop now.
        return await next(context);
    }

    private void RouteToDlq(ConsumeResult<TKey, TValue> cr, MessageContext<TKey, TValue> ctx) {
        //Debugger.Break();
        // TODO: real DLQ implementation
        // This is where you'd:
        //  - publish to a DLQ topic
        //  - log payload and error details
        //  - tag in OTEL as a DLQ event
    }

    public void Dispose() => consumer.Close();

    public ValueTask DisposeAsync() {
        if (consumer is IAsyncDisposable asyncDisposable) {
            return asyncDisposable.DisposeAsync();
        }
        return ValueTask.CompletedTask;
    }
}
