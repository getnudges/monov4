namespace UserApi;

public static class LoggingExtensions {
    private static readonly Action<ILogger<Mutation>, Exception> MutationException =
        LoggerMessage.Define(LogLevel.Critical, new EventId(100, nameof(MutationException)), "GraphQL Mutation Error");
    private static readonly Action<ILogger<Mutation>, string, Exception?> MessageSendError =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(200, nameof(MessageSendError)), "Message Send Error: {Message}");
    private static readonly Action<ILogger<LoggerExecutionEventListener>, Exception?> GraphqlError =
        LoggerMessage.Define(LogLevel.Error, new EventId(201, nameof(GraphqlError)), "GraphQL Request Error");
    private static readonly Action<ILogger, Exception?> AuthFailure =
        LoggerMessage.Define(LogLevel.Warning, new EventId(300, nameof(AuthFailure)), "Auth Failure");

    internal static void LogException(this ILogger<Mutation> logger, Exception ex) =>
        MutationException(logger, ex);
    internal static void LogMessageSendError(this ILogger<Mutation> logger, string message) =>
        MessageSendError(logger, message, null);
    internal static void LogGraphqlError(this ILogger<LoggerExecutionEventListener> logger, Exception ex) =>
        GraphqlError(logger, ex);
    internal static void LogAuthFailure(this ILogger logger, Exception ex) =>
        AuthFailure(logger, ex);
}
