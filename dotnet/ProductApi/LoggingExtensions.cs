namespace ProductApi;

public static partial class LoggingExtensions {
    [LoggerMessage(Level = LogLevel.Critical, Message = "GraphQL Mutation Error")]
    public static partial void LogException(this ILogger<Mutation> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Plan {Name} ({Id}) Created")]
    public static partial void LogPlanCreated(this ILogger<Mutation> logger, int id, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Plan {Id} Created")]
    public static partial void LogPlanSubscriptionCreated(this ILogger<Mutation> logger, Guid id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Plan {Name} ({Id}) Deleted")]
    public static partial void LogPlanDeleted(this ILogger<Mutation> logger, int id, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Price Tier {Name} ({Id}) Deleted")]
    public static partial void LogPriceTierDeleted(this ILogger<Mutation> logger, int id, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Kafka message sent to topic {Topic}")]
    public static partial void LogKafkaMessageSent(this ILogger<Mutation> logger, string topic);

    [LoggerMessage(Level = LogLevel.Error, Message = "GraphQL Request Error")]
    public static partial void LogGraphqlError(this ILogger<LoggerExecutionEventListener> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Auth Failure")]
    public static partial void LogAuthFailure(this ILogger logger, Exception ex);
}
