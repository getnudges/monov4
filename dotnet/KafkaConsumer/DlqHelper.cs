using System.Diagnostics;
using Nudges.Kafka;
using Nudges.Kafka.Events;
using Nudges.Kafka.Middleware;

namespace KafkaConsumer;

public static class DlqHelper {
    public static DlqHandler<TKey, TValue> CreateDlqHandler<TKey, TValue>(
        KafkaMessageProducer<DeadLetterEventKey, DeadLetterEvent> dlqProducer) =>
            async (consumeResult, context) => {
                var exception = context.Exception ?? new DlqContextException("No exception found in message context");
                var (exType, exMsg, stackTrace, innerType, innerMsg, innerStackTrace) =
                    ExceptionHelper.ExtractExceptionData(exception);

                // Extract original message payloads
                var originalKeyJson = MessagePayloadHelper.SerializeToJson(consumeResult.Message.Key);
                var originalValueJson = MessagePayloadHelper.SerializeToJson(consumeResult.Message.Value);
                var originalHeaders = MessagePayloadHelper.ExtractHeaders(consumeResult);

                // Create DLQ key
                var eventKey = consumeResult.Message.Key switch {
                    EventKey ek => ek,
                    _ => new EventKey(consumeResult.Message.Key?.ToString() ?? "unknown")
                };

                var dlqKey = DeadLetterEventKey.MessageFailed(eventKey);

                // Create DLQ event
                var dlqEvent = new DeadLetterEvent {
                    ExceptionType = exType,
                    ExceptionMessage = exMsg,
                    StackTrace = stackTrace,
                    InnerExceptionType = innerType,
                    InnerExceptionMessage = innerMsg,
                    InnerExceptionStackTrace = innerStackTrace,
                    Topic = consumeResult.Topic,
                    EventKey = dlqKey.EventKey,
                    OriginalKeyJson = originalKeyJson,
                    OriginalValueJson = originalValueJson,
                    OriginalHeaders = originalHeaders,
                    AttemptCount = context.AttemptCount,
                    FailureType = context.Failure
                };

                // Produce to DLQ topic
                await dlqProducer.Produce(dlqKey, dlqEvent, context.CancellationToken);

                // Tag in OpenTelemetry (already done in MessageProcessor, but adding topic for reference)
                Activity.Current?.SetTag("dlq.topic", Topics.DeadLetter);
            };
}
