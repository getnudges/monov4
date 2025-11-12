using Microsoft.Extensions.Logging;

namespace Nudges.Auth.Keycloak;

internal static partial class KeycloakOidcClientLogs {
    [LoggerMessage(Level = LogLevel.Warning, Message = "Request Failed")]
    public static partial void LogRequestError(this ILogger<KeycloakOidcClient> logger, Exception? exception = null);
}
