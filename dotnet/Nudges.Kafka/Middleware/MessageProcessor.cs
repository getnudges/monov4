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
    private const int MaxRetryAttempts = 3;

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

            while (!cancellationToken.IsCancellationRequested) {
                // 1) Track how many times we've tried this message
                context = context with { AttemptCount = context.AttemptCount + 1 };

                // 2) Run the middleware pipeline once
                context = await ProcessMessageAsync(context);

                // 3) Success → commit and move on
                if (context.Failure == FailureType.None) {
                    consumer.Commit(cr);
                    break;
                }

                // 4) Permanent failure → DLQ + commit + stop retrying
                if (context.Failure == FailureType.Permanent) {
                    RouteToDlq(cr, context);
                    consumer.Commit(cr);
                    break;
                }

                // 5) Too many attempts → treat as poison and DLQ
                if (context.AttemptCount >= MaxRetryAttempts) {
                    RouteToDlq(cr, context);
                    consumer.Commit(cr);
                    break;
                }

                // 6) Transient or DependencyDown → backoff and retry
                await ApplyBackoff(context.AttemptCount, cancellationToken);
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

    private static async Task ApplyBackoff(int attempt, CancellationToken ct) {
        // Exponential backoff with jitter:
        // 1s, 2s, 4s + up to 250ms random jitter
        var baseDelay = Math.Pow(2, attempt - 1); // attempt starts at 1
        var jitter = Random.Shared.Next(0, 250);
        var delay = TimeSpan.FromSeconds(baseDelay) + TimeSpan.FromMilliseconds(jitter);

        Activity.Current?.AddEvent(new ActivityEvent("Backoff delay",
            tags: new ActivityTagsCollection {
                        { "retry.backoff_ms", delay.TotalMilliseconds }
            }));
        await Task.Delay(delay, ct);
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
