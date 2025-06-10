namespace LocalizationApi.Services;

public static partial class LocalizationServiceLogs {
    [LoggerMessage(1, LogLevel.Information, "Requesting localized string for key: {ResourceKey}")]
    public static partial void RequestingLocalizedString(this ILogger<LocalizationService> logger, string resourceKey);
    [LoggerMessage(2, LogLevel.Warning, "Resource not found for key: {ResourceKey}")]
    public static partial void ResourceNotFound(this ILogger<LocalizationService> logger, string resourceKey);
}
