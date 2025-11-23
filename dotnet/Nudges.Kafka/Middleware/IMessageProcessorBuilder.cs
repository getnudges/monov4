using Microsoft.Extensions.Logging;

namespace Nudges.Kafka.Middleware;

public interface IMessageProcessorBuilder<TKey, TValue> {
    public IMessageProcessorBuilder<TKey, TValue> Use(IMessageMiddleware<TKey, TValue> middleware);
    public MessageProcessorOptions Options { get; }
    public IMessageProcessor<TKey, TValue> Build();
}

/// <summary>
/// Options for configuring a <see cref="MessageProcessor{TKey, TValue}"/>.
/// </summary>
/// <param name="Topic">Message topic to consume messages from</param>
/// <param name="BootstrapServers">URI(s) of servers to connect to</param>
/// <param name="AllowAutoCreateTopics">(See <seealso cref="Confluent.Kafka.ClientConfig.AllowAutoCreateTopics"/>)</param>
/// <param name="StoppingToken">Token used to stop underlying consumer from blocking.</param>
public record class MessageProcessorOptions(string Topic, string BootstrapServers, bool AllowAutoCreateTopics, CancellationToken? StoppingToken = default);

public static class MessageProcessorBuilderExtensions {

    public static IMessageProcessorBuilder<TKey, TValue> UseCircuitBreaker<TKey, TValue>(
        this IMessageProcessorBuilder<TKey, TValue> builder,
        TimeProvider timeProvider,
        int failureThreshold = 5,
        TimeSpan? openInterval = null,
        TimeSpan? rollingWindow = null) {
        var circuitBreaker = new CircuitBreakerMiddleware<TKey, TValue>(
            timeProvider,
            failureThreshold,
            openInterval,
            rollingWindow);
        return builder.Use(circuitBreaker);
    }

    public static IMessageProcessorBuilder<TKey, TValue> UseRetry<TKey, TValue>(
        this IMessageProcessorBuilder<TKey, TValue> builder,
        TimeProvider timeProvider,
        int maxRetries = 3,
        Func<int, TimeSpan>? backoff = null) {
        var retry = new RetryMiddleware<TKey, TValue>(
            timeProvider,
            maxRetries,
            backoff);
        return builder.Use(retry);
    }

    public static IMessageProcessorBuilder<TKey, TValue> UseErrorHandling<TKey, TValue>(
        this IMessageProcessorBuilder<TKey, TValue> builder,
        ILogger<ErrorHandlingMiddleware<TKey, TValue>> logger) {
        var errorHandling = new ErrorHandlingMiddleware<TKey, TValue>(logger);
        return builder.Use(errorHandling);
    }

    public static IMessageProcessorBuilder<TKey, TValue> ErrorHandlingMiddleware<TKey, TValue>(
        this IMessageProcessorBuilder<TKey, TValue> builder,
        ILogger<ErrorHandlingMiddleware<TKey, TValue>> logger) {
        var errorHandling = new ErrorHandlingMiddleware<TKey, TValue>(logger);
        return builder.Use(errorHandling);
    }

    public static IMessageProcessorBuilder<TKey, TValue> UseTracing<TKey, TValue>(
        this IMessageProcessorBuilder<TKey, TValue> builder) {
        var tracing = new TracingMiddleware<TKey, TValue>();
        return builder.Use(tracing);
    }
}
