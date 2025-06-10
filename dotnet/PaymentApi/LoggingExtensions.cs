namespace PaymentApi;

public static class LoggingExtensions {
    private static readonly Action<ILogger<LoggerExecutionEventListener>, Exception?> GraphqlError =
        LoggerMessage.Define(LogLevel.Error, new EventId(201, nameof(GraphqlError)), "GraphQL Request Error");
    private static readonly Action<ILogger, Exception?> AuthFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(300, nameof(AuthFailure)), "Auth Failure");

    internal static void LogGraphqlError(this ILogger<LoggerExecutionEventListener> logger, Exception ex) =>
        GraphqlError(logger, ex);
    internal static void LogAuthFailure(this ILogger logger, Exception ex) =>
        AuthFailure(logger, ex);
}
