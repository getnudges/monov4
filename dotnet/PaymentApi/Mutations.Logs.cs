namespace PaymentApi;

internal static partial class MutationsLogs {

    [LoggerMessage(LogLevel.Information, "Creating checkout session for {ClientId} and product {PriceForeignServiceId}")]
    public static partial void LogCreatingCheckoutSession(this ILogger<Mutation> logger, Guid clientId, string PriceForeignServiceId);

    [LoggerMessage(LogLevel.Information, "Created checkout session for {CustomerId} and product {PriceForeignServiceId}")]
    public static partial void LogCreatedCheckoutSession(this ILogger<Mutation> logger, string customerId, string PriceForeignServiceId);

    [LoggerMessage(LogLevel.Error, "Created checkout session for {CustomerId} and product {PriceForeignServiceId} failed")]
    public static partial void LogCreateCheckoutSessionFailed(this ILogger<Mutation> logger, string customerId, string PriceForeignServiceId, Exception exception);

    [LoggerMessage(LogLevel.Information, "Canceling checkout session {SessionId}")]
    public static partial void LogCancelingCheckoutSession(this ILogger<Mutation> logger, string sessionId);

    [LoggerMessage(LogLevel.Information, "Canceled checkout session {SessionId}")]
    public static partial void LogCanceledCheckoutSession(this ILogger<Mutation> logger, string sessionId);

    [LoggerMessage(LogLevel.Information, "Completing checkout session {SessionId}")]
    public static partial void LogCompletingCheckoutSession(this ILogger<Mutation> logger, string sessionId);

    [LoggerMessage(LogLevel.Information, "Completed checkout session {SessionId}")]
    public static partial void LogCompletedCheckoutSession(this ILogger<Mutation> logger, string sessionId);
}
