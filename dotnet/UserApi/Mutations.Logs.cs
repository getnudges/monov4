using Microsoft.Extensions.Logging;

namespace UserApi;

public static partial class MutationLogs {
    [LoggerMessage(Level = LogLevel.Critical, Message = "GraphQL Mutation Error")]
    public static partial void LogException(this ILogger<Mutation> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message Send Error: {Message}")]
    public static partial void LogMessageSendError(this ILogger<Mutation> logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "User created in database {UserId} in {DurationMs}ms")]
    public static partial void LogUserCreated(this ILogger<Mutation> logger, Guid userId, double durationMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client created in database {ClientId} ({ClientName}) in {DurationMs}ms")]
    public static partial void LogClientCreated(this ILogger<Mutation> logger, Guid clientId, string clientName, double durationMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Produced ClientCreated event for Client {ClientId} to {Topic} @ {Offset}")]
    public static partial void LogClientCreatedEventProduced(this ILogger<Mutation> logger, Guid clientId, string topic, long offset);

    [LoggerMessage(Level = LogLevel.Error, Message = "CreateClient failed for name={ClientName}")]
    public static partial void LogClientCreationFailed(this ILogger<Mutation> logger, string clientName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "GraphQL Request Error")]
    public static partial void LogGraphqlError(this ILogger<LoggerExecutionEventListener> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Auth Failure")]
    public static partial void LogAuthFailure(this ILogger logger, Exception ex);
}
