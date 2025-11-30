using Nudges.Configuration;

namespace UserApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string CacheServerAddress = "WarpCache__Url";
    [ConfigurationKey]
    public const string SubscribeHost = "SUBSCRIBE_HOST";
    [ConfigurationKey]
    public const string KafkaBrokerList = "Kafka__BrokerList";
    [ConfigurationKey]
    public const string OtlpEndpointUrl = "Otlp__Endpoint";
}
