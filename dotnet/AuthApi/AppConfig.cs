using Nudges.Configuration;

namespace AuthApi;

internal static class AppConfig {
    [ConfigurationKey]
    public const string OidcServerUrl = "Oidc:ServerUrl";
    [ConfigurationKey]
    public const string OidcRealm = "Oidc:Realm";
    [ConfigurationKey]
    public const string OidcClientId = "Oidc:ClientId";
    [ConfigurationKey]
    public const string OidcClientSecret = "Oidc:ClientSecret";
    [ConfigurationKey]
    public const string OidcAdminUsername = "Oidc:AdminCredentials:Username";
    [ConfigurationKey]
    public const string OidcAdminPassword = "Oidc:AdminCredentials:Password";
    [ConfigurationKey]
    public const string CacheServerAddress = "CACHE_SERVER_ADDRESS";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
    [ConfigurationKey]
    public const string OltpEndpointUrl = "OTLP_ENDPOINT_URL";
    [ConfigurationKey]
    public const string OidcServerAuthUrl = "OIDC_SERVER_AUTH_URL";
    [ConfigurationKey]
    public const string IgnoreSslCertValidation = "IGNORE_SSL_CERT_VALIDATION";
}
