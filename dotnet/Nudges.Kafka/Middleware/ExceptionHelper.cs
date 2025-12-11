namespace Nudges.Kafka.Middleware;

public static class ExceptionHelper {
    private const int MaxStackTraceLength = 8000;

    public static (string Type, string Message, string? StackTrace,
                   string? InnerType, string? InnerMessage, string? InnerStackTrace)
        ExtractExceptionData(Exception exception) {

        ArgumentNullException.ThrowIfNull(exception);

        var type = exception.GetType().FullName ?? exception.GetType().Name;
        var message = exception.Message;
        var stackTrace = TruncateStackTrace(exception.StackTrace);

        string? innerType = null;
        string? innerMessage = null;
        string? innerStackTrace = null;

        if (exception.InnerException is not null) {
            innerType = exception.InnerException.GetType().FullName ?? exception.InnerException.GetType().Name;
            innerMessage = exception.InnerException.Message;
            innerStackTrace = TruncateStackTrace(exception.InnerException.StackTrace);
        }

        return (type, message, stackTrace, innerType, innerMessage, innerStackTrace);
    }

    private static string? TruncateStackTrace(string? stackTrace) {
        if (stackTrace is null) {
            return null;
        }

        if (stackTrace.Length <= MaxStackTraceLength) {
            return stackTrace;
        }

        return stackTrace[..MaxStackTraceLength] + "\n... (truncated)";
    }
}
