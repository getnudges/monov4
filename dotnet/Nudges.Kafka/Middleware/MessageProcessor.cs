using System.Diagnostics;
using System.Runtime.CompilerServices;
using Confluent.Kafka;

namespace Nudges.Kafka.Middleware;

public record MessageContext<TKey, TValue>(
    ConsumeResult<TKey, TValue> ConsumeResult,
    CancellationToken CancellationToken,
    int AttemptCount = 0,
    FailureType Failure = FailureType.None,
    Exception? Exception = null);

public enum FailureType {
    None,           // No failure, message processed successfully
    Transient,      // Might succeed on retry (timeouts, network blips, etc.)
    Permanent,      // Will never succeed (validation errors, bad payload)
    DependencyDown, // A downstream system is unavailable (DB/Kafka/Keycloak/etc.)
    Fatal           // Unrecoverable error, do not retry
}


public delegate Task<MessageContext<TKey, TValue>> MessageHandler<TKey, TValue>(MessageContext<TKey, TValue> context);
public delegate Task DlqHandler<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult, MessageContext<TKey, TValue> context);

public sealed class MessageProcessor<TKey, TValue>(
    IAsyncConsumer<TKey, TValue> consumer,
    IEnumerable<IMessageMiddleware<TKey, TValue>> middlewares,
    int maxRetryAttempts = 5,
    DlqHandler<TKey, TValue>? dlqHandler = null)
    : IMessageProcessor<TKey, TValue>, IAsyncDisposable {

    // How many times we're willing to retry before giving up
    private readonly int _maxRetryAttempts = maxRetryAttempts;
    private readonly DlqHandler<TKey, TValue>? _dlqHandler = dlqHandler;

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
                    if (context.AttemptCount >= _maxRetryAttempts) {
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

        return await next(context);
    }

    private async void RouteToDlq(ConsumeResult<TKey, TValue> cr, MessageContext<TKey, TValue> ctx) {
        if (_dlqHandler is null) {
            // No DLQ configured - just return
            return;
        }

        try {
            await _dlqHandler(cr, ctx);

            // Tag in OpenTelemetry
            Activity.Current?.SetTag("message.routed_to_dlq", true);
        } catch (Exception ex) {
            // DLQ routing failed - log but don't throw (we still need to commit the message)
            Activity.Current?.AddException(ex);
            Activity.Current?.SetTag("dlq.routing_failed", true);
        }
    }

    public void Dispose() => consumer.Close();

    public ValueTask DisposeAsync() {
        if (consumer is IAsyncDisposable asyncDisposable) {
            return asyncDisposable.DisposeAsync();
        }
        return ValueTask.CompletedTask;
    }
}
