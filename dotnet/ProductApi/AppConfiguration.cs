using Nudges.Configuration;

namespace ProductApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string KafkaBrokerList = "KAFKA_BROKER_LIST";
    [ConfigurationKey]
    public const string OtlpEndpointUrl = "OTLP_ENDPOINT_URL";
}
