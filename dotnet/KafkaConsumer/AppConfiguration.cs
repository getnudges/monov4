using Nudges.Configuration;

namespace KafkaConsumer;

internal static class AppConfiguration {
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
    public const string AuthApikey = "AUTH_API_KEY";
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string CacheServerAddress = "CACHE_SERVER_ADDRESS";
    [ConfigurationKey]
    public const string StripeApiKey = "STRIPE_API_KEY";
    [ConfigurationKey]
    public const string StripeApiUrl = "STRIPE_API_URL";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
    [ConfigurationKey]
    public const string GraphQLApiUrl = "GRAPHQL_API_URL";
    [ConfigurationKey]
    public const string TwilioAccountSid = "TWILIO_ACCOUNT_SID";
    [ConfigurationKey]
    public const string TwilioAuthToken = "TWILIO_AUTH_TOKEN";
    [ConfigurationKey]
    public const string TwilioMessageServiceSid = "TWILIO_MESSAGE_SERVICE_SID";
    [ConfigurationKey]
    public const string AuthApiUrl = "AUTH_API_URL";
    [ConfigurationKey]
    public const string LocalizationApiUrl = "LOCALIZATION_API_URL";
}
