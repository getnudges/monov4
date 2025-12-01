using Nudges.Configuration;

namespace KafkaConsumer;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string AuthApikey = "AUTH_API_KEY";
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string StripeApiKey = "STRIPE_API_KEY";
    [ConfigurationKey]
    public const string StripeApiUrl = "STRIPE_API_URL";
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
