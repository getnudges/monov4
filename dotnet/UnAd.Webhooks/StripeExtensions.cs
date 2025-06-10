using System.Diagnostics;
using Stripe;

namespace UnAd.Webhooks;
internal static class StripeExtensions {
    private static readonly string[] ActiveSubscriptionStatuses = ["active", "trialing"];

    public static bool IsActive(this Subscription subscription) =>
        ActiveSubscriptionStatuses.Contains(subscription.Status);

    public static Dictionary<string, string>? GetSubscriptionProductMetaData(this Subscription subscription) =>
        subscription.Items.Data.FirstOrDefault()?.Price.Product.Metadata;

    public static Activity? GetActivity(this EventRequest eventRequest, ActivitySource source, string name) {
        if (eventRequest.IdempotencyKey is string key && ActivityContext.TryParse(key, default, out var context)) {
            return source.CreateActivity(name, ActivityKind.Server, context);
        }
        return Activity.Current;
    }
}
