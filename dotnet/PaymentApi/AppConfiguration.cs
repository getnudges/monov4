using Nudges.Configuration;

namespace PaymentApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string SimpleAuthSigningKey = "Authentication:Schemes:Simple:IssuerSigningKey";
    [ConfigurationKey]
    public const string ServerAuthSigningKey = "Authentication:Schemes:Server:IssuerSigningKey";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string StripeApiKey = "STRIPE_API_KEY";
    [ConfigurationKey]
    public const string StripeApiUrl = "STRIPE_API_URL";
}
