using Nudges.Configuration;

namespace UserApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
    [ConfigurationKey]
    public const string SubscribeHost = "SUBSCRIBE_HOST";
}
