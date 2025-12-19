namespace AuthApi;

public static partial class HandlersLogs {

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Missing required role claim."
    )]
    public static partial void LogMissingClaim(this ILogger<ApiController> logger);
}
