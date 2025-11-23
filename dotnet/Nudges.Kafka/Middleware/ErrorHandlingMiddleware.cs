using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Nudges.Kafka.Middleware;

public sealed class ErrorHandlingMiddleware<TKey, TValue>(
    ILogger<ErrorHandlingMiddleware<TKey, TValue>> logger)
    : IMessageMiddleware<TKey, TValue> {

    public async Task<MessageContext<TKey, TValue>> InvokeAsync(
        MessageContext<TKey, TValue> context,
        MessageHandler<TKey, TValue> next) {

        try {
            return await next(context);
        } catch (Exception ex) {
            var failureType = Classify(ex);

            logger.LogMessageProcessingError(ex,
                context.ConsumeResult.Topic,
                context.ConsumeResult.Partition.Value,
                context.ConsumeResult.Offset.Value,
                failureType);

            return context with { Failure = failureType };
        }
    }

    private static FailureType Classify(Exception ex) => ex switch {
        TimeoutException => FailureType.Transient,
        TaskCanceledException => FailureType.Transient,
        OperationCanceledException => FailureType.Transient,

        KafkaException => FailureType.DependencyDown,

        // Domain-specific exceptions you might add later
        // ValidationException   => FailureType.Permanent,
        HttpRequestException => FailureType.DependencyDown,

        _ => FailureType.Transient
    };
}
