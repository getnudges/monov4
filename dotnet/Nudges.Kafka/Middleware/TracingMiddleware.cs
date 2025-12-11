using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nudges.Kafka.Middleware;

public class TracingMiddleware<TKey, TValue> : IMessageMiddleware<TKey, TValue> {

    private static readonly ActivitySource ActivitySource = new($"{typeof(TracingMiddleware<,>).Namespace}.TracingMiddleware");
    private static readonly Meter Meter = new($"{typeof(TracingMiddleware<,>).Namespace}.MessageProcessor");
    private static readonly Counter<long> MessagesProcessed = Meter.CreateCounter<long>("messages_processed_total");
    private static readonly Histogram<double> MessageProcessingTime = Meter.CreateHistogram<double>("message_processing_time_seconds");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("message_retries_total");

    public async Task<MessageContext<TKey, TValue>> InvokeAsync(MessageContext<TKey, TValue> context, MessageHandler<TKey, TValue> next) {
        using var activity = ActivitySource.StartActivity(
            $"Process_{typeof(TValue).Name}",
            ActivityKind.Consumer,
            context.ConsumeResult.Message.GetActivityContext() ?? default,
            [
                new KeyValuePair<string, object?>("kafka.topic", context.ConsumeResult.Topic),
                new KeyValuePair<string, object?>("kafka.partition", context.ConsumeResult.Partition),
                new KeyValuePair<string, object?>("kafka.offset", context.ConsumeResult.Offset),
                new KeyValuePair<string, object?>("message.key", context.ConsumeResult.Message.Key?.ToString()),
                new KeyValuePair<string, object?>("retry.attempt", context.AttemptCount),
            ]
        );
        activity?.Start();

        if (context.AttemptCount > 1) {
            activity?.AddEvent(new ActivityEvent(
                $"Retry attempt #{context.AttemptCount}",
                tags: new ActivityTagsCollection {
            { "retry.attempt", context.AttemptCount }
                }));
            RetryCounter.Add(1);
        }

        var startTime = Stopwatch.GetTimestamp();
        try {
            var result = await next(context);
            if (result.Failure == FailureType.None) {
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("delivery.outcome", "success");
                MessagesProcessed.Add(1, [new("status", "success")]);
            } else if (result.Failure == FailureType.Transient) {
                activity?.SetTag("delivery.outcome", "retrying");
            } else if (result.Failure == FailureType.Permanent) {
                activity?.SetTag("delivery.outcome", "dlq");
            } else if (result.Failure == FailureType.DependencyDown) {
                activity?.SetTag("delivery.outcome", "skipped-breaker-open");
            } else {
                activity?.SetStatus(ActivityStatusCode.Error, "Message processing failed");
                MessagesProcessed.Add(1, [new("status", "failure")]);
            }
            return result;
        } catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            MessagesProcessed.Add(1, [new("status", "error")]);
            throw;
        } finally {
            var endTime = Stopwatch.GetTimestamp();
            var duration = (endTime - startTime) / (double)Stopwatch.Frequency;
            MessageProcessingTime.Record(duration);
            activity?.Stop();
        }
    }
}

