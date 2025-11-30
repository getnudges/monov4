using Nudges.Configuration;

namespace AuthApi;

internal static class AppConfig {
    [ConfigurationKey(optional: true)]
    public const string IgnoreSslCertValidation = "IGNORE_SSL_CERT_VALIDATION";
}
