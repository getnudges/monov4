using Nudges.Configuration;

namespace ProductApi;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string RedisUrl = "REDIS_URL";
}
