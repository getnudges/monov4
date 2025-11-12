using Microsoft.Extensions.Logging;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer.Middleware;

public class SmartErrorHandlingMiddleware<TKey, TValue>(
    ILogger<SmartErrorHandlingMiddleware<TKey, TValue>> logger,
    KafkaMessageProducer<DeadLetterEventKey, DeadLetterEvent> dlqProducer)
    : ErrorHandlingMiddleware<TKey, TValue> {

    protected override MessageContext<TKey, TValue> OnError(
        MessageContext<TKey, TValue> context,
        Exception ex) {

        var strategy = DetermineStrategy(ex, context.AttemptCount);

        return strategy switch {
            ErrorStrategy.Retry => HandleRetry(context, ex),
            ErrorStrategy.Skip => HandleSkip(context, ex),
            ErrorStrategy.DeadLetter => HandleDeadLetter(context, ex),
            _ => context with { Failed = true }
        };
    }

    private ErrorStrategy DetermineStrategy(Exception ex, int attemptCount) =>
        ex switch {
            // Transient errors - retry
            HttpRequestException => attemptCount < 3
                ? ErrorStrategy.Retry
                : ErrorStrategy.DeadLetter,
            TimeoutException => attemptCount < 3
                ? ErrorStrategy.Retry
                : ErrorStrategy.DeadLetter,

            UnhandledMessageException => ErrorStrategy.DeadLetter,

            ArgumentException => ErrorStrategy.DeadLetter,

            // Unknown - be cautious, retry a few times
            _ => attemptCount < 2
                ? ErrorStrategy.Retry
                : ErrorStrategy.DeadLetter
    };

    private MessageContext<TKey, TValue> HandleRetry(
        MessageContext<TKey, TValue> context,
        Exception ex) {

        var delay = TimeSpan.FromSeconds(Math.Pow(2, context.AttemptCount));
        Task.Delay(delay, context.CancellationToken).Wait();

        logger.LogWarning(ex,
            "Retrying message {Key} after {Delay}s (attempt {Attempt})",
            context.ConsumeResult.Message.Key,
            delay.TotalSeconds,
            context.AttemptCount);

        // Return without committing, will retry
        return context with { Failed = false };
    }

    private MessageContext<TKey, TValue> HandleSkip(
        MessageContext<TKey, TValue> context,
        Exception ex) {

        logger.LogError(ex,
            "Skipping message {Key} - permanent failure",
            context.ConsumeResult.Message.Key);

        // Commit to skip this message
        return context with { Failed = false };
    }

    private MessageContext<TKey, TValue> HandleDeadLetter(
        MessageContext<TKey, TValue> context,
        Exception ex) {

        logger.LogError(ex,
            "Sending message {Key} to DLQ after {Attempts} attempts",
            context.ConsumeResult.Message.Key,
            context.AttemptCount);

        // Send to DLQ (fire-and-forget or with error handling)
        _ = SendToDeadLetterQueueAsync(context, ex);

        // Commit to move past this message
        return context with { Failed = false };
    }

    private async Task SendToDeadLetterQueueAsync(MessageContext<TKey, TValue> context, Exception ex) =>
        await dlqProducer.ProduceMessageFailed(context.ConsumeResult.Topic, EventKey.Parse(context.ConsumeResult.Message.Key!.ToString()!), ex);
}

public enum ErrorStrategy {
    Retry,
    Skip,
    DeadLetter
}
