using Nudges.Configuration;

namespace UserApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string CacheServerAddress = "CACHE_SERVER_ADDRESS";
    [ConfigurationKey]
    public const string SubscribeHost = "SUBSCRIBE_HOST";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
    [ConfigurationKey]
    public const string OltpEndpointUrl = "OTLP_ENDPOINT_URL";
}
