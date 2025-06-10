using UnAd.Configuration;

namespace UnAd.Webhooks;

internal static class AppConfiguration {

    public const string ResourcesPath = "Resources";

    [ConfigurationKey]
    public const string ApiKey = "API_KEY";
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string CacheServerAddress = "CACHE_SERVER_ADDRESS";
    [ConfigurationKey]
    public const string TwilioAccountSid = "TWILIO_ACCOUNT_SID";
    [ConfigurationKey]
    public const string TwilioAuthToken = "TWILIO_AUTH_TOKEN";
    [ConfigurationKey]
    public const string TwilioMessageServiceSid = "TWILIO_MESSAGE_SERVICE_SID";
    [ConfigurationKey]
    public const string StripeApiKey = "STRIPE_API_KEY";
    [ConfigurationKey]
    public const string StripeApiUrl = "STRIPE_API_URL";
    [ConfigurationKey]
    public const string StripeWebhookEndpointSecret = "STRIPE_WEBHOOKS_SECRET";
    [ConfigurationKey]
    public const string AccountUrl = "AccountUrl";
    [ConfigurationKey]
    public const string StripePortalUrl = "StripePortalUrl";
    [ConfigurationKey]
    public const string ClientLinkBaseUri = "ClientLinkBaseUri";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
    [ConfigurationKey]
    public const string GraphQLApiUrl = "GRAPHQL_API_URL";
    [ConfigurationKey]
    public const string AuthApiUrl = "AUTH_API_URL";
    [ConfigurationKey]
    public const string AuthApiKey = "AUTH_API_KEY";
    [ConfigurationKey]
    public const string LocalizationApiUrl = "LOCALIZATION_API_URL";
    [ConfigurationKey]
    public const string OidcServerUrl = "Keycloak:auth-server-url";
    [ConfigurationKey]
    public const string OidcRealm = "Keycloak:realm";
    [ConfigurationKey]
    public const string OidcResource = "Keycloak:resource";
    [ConfigurationKey]
    public const string OidcClientSercret = "Keycloak:credentials:secret";
    [ConfigurationKey]
    public const string AuthApikey = "AUTH_API_KEY";
}
