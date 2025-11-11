using Microsoft.Extensions.Logging;

namespace Nudges.Auth.Keycloak;
internal static partial class KeycloakOidcClientLogs {
    [LoggerMessage(Level = LogLevel.Warning)]
    public static partial void LogRequestError(this ILogger<KeycloakOidcClient> logger, string message, Exception? exception = null);
}
