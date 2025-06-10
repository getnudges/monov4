using Stripe;

namespace Nudges.Stripe;
internal static class StripeExtensions {
    private static readonly string[] ActiveSubscriptionStatuses = ["active", "trialing"];

    public static bool IsActive(this Subscription subscription) =>
        ActiveSubscriptionStatuses.Contains(subscription.Status);

    public static Dictionary<string, string>? GetSubscriptionProductMetaData(this Subscription subscription) =>
        subscription.Items.Data.FirstOrDefault()?.Price.Product.Metadata;
}
