using Microsoft.Extensions.Configuration;

namespace DbSeeder;

internal static class AppConfiguration {

    public static class Keys {
        public const string RedisUrl = "REDIS_URL";
        public const string StripeApiKey = "STRIPE_API_KEY";
    }

    public static string GetStripeApiKey(this IConfiguration configuration) =>
        configuration[Keys.StripeApiKey] ?? throw new ConfigurationValueMissingException(Keys.StripeApiKey);

    public static string GetRedisUrl(this IConfiguration configuration) =>
        configuration[Keys.RedisUrl] ?? throw new ConfigurationValueMissingException(Keys.RedisUrl);
}
