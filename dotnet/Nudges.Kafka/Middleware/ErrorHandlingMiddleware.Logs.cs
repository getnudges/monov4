using Microsoft.Extensions.Logging;

namespace Nudges.Kafka.Middleware;

public static partial class ErrorHandlingMiddlewareLogs {

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Error processing Kafka message from topic {Topic}, partition {Partition}, offset {Offset}. Classified as {FailureType}."
    )]
    public static partial void LogMessageProcessingError(
        this ILogger logger,
        Exception exception,
        string topic,
        int partition,
        long offset,
        FailureType failureType);
}
