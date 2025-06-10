using Nudges.Configuration;

namespace ProductApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string SimpleAuthSigningKey = "Authentication:Schemes:Simple:IssuerSigningKey";
    [ConfigurationKey]
    public const string ServerAuthSigningKey = "Authentication:Schemes:Server:IssuerSigningKey";
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string CognitoAuthority = "Cognito__Authority";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
}
