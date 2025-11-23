using System.Diagnostics;

namespace Nudges.Kafka.Middleware;

public sealed class RetryMiddleware<TKey, TValue>(
    TimeProvider timeProvider,
    int maxRetries = 3,
    Func<int, TimeSpan>? backoff = null
) : IMessageMiddleware<TKey, TValue> {
    private static readonly ActivitySource ActivitySource =
        new($"{typeof(RetryMiddleware<,>).Namespace}.RetryMiddleware");

    public async Task<MessageContext<TKey, TValue>> InvokeAsync(
        MessageContext<TKey, TValue> context,
        MessageHandler<TKey, TValue> next) {
        var attempt = 0;
        var start = timeProvider.GetUtcNow();

        while (true) {
            attempt++;

            using var retryActivity = ActivitySource.StartActivity(
                $"retry_attempt_{attempt}",
                ActivityKind.Internal,
                context.ConsumeResult.Message.GetActivityContext() ?? default);

            retryActivity?.SetTag("kafka.retry.attempt", attempt);
            retryActivity?.SetTag("kafka.retry.max", maxRetries);
            retryActivity?.SetTag("kafka.retry.elapsed_ms",
                (timeProvider.GetUtcNow() - start).TotalMilliseconds);

            var result = await next(context with { AttemptCount = attempt });

            if (result.Failure == FailureType.None) {
                retryActivity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }

            retryActivity?.SetStatus(ActivityStatusCode.Error);
            retryActivity?.SetTag("kafka.retry.reason", result.Failure.ToString());

            if (result.Failure == FailureType.Fatal || attempt >= maxRetries) {
                retryActivity?.AddEvent(new ActivityEvent("retry_abandoned"));
                return result;
            }

            retryActivity?.AddEvent(new ActivityEvent("retry_scheduled"));

            var delay = backoff?.Invoke(attempt)
                        ?? TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);

            await Task.Delay(delay, context.CancellationToken);
        }
    }
}
