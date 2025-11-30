using Nudges.Configuration;

namespace ProductApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string KafkaBrokerList = "Kafka__BrokerList";
    [ConfigurationKey]
    public const string OtlpEndpointUrl = "Otlp__Endpoint";
}
