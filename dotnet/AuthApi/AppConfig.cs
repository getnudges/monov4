using Nudges.Configuration;

namespace AuthApi;

internal static class AppConfig {
    [ConfigurationKey]
    public const string OidcServerUrl = "Oidc__ServerUrl";
    [ConfigurationKey]
    public const string OidcRealm = "Oidc__Realm";
    [ConfigurationKey]
    public const string OidcClientId = "Oidc__ClientId";
    [ConfigurationKey]
    public const string OidcClientSecret = "Oidc__ClientSecret";
    [ConfigurationKey]
    public const string OidcAdminUsername = "Oidc__AdminCredentials__Username";
    [ConfigurationKey]
    public const string OidcAdminPassword = "Oidc__AdminCredentials__Password";
    [ConfigurationKey]
    public const string CacheServerAddress = "CACHE_SERVER_ADDRESS";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
    [ConfigurationKey]
    public const string OltpEndpointUrl = "OTLP_ENDPOINT_URL";
    [ConfigurationKey]
    public const string OidcServerAuthUrl = "OIDC_SERVER_AUTH_URL";
    [ConfigurationKey(false)]
    public const string IgnoreSslCertValidation = "IGNORE_SSL_CERT_VALIDATION";
}
