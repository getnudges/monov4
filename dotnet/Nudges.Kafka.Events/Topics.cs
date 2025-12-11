namespace Nudges.Kafka.Events;

public static class Topics {
    public const string Clients = "clients";
    public const string Notifications = "notifications";
    public const string Payments = "payments";
    public const string Plans = "plans";
    public const string PlanSubscriptions = "plan-subscriptions";
    public const string DeadLetter = "deadletter";
    public const string Subscriptions = "subscriptions";
    public const string PriceTiers = "price-tiers";
    public const string DiscountCodes = "discount-codes";
    public const string UserAuthentication = "user-authentication";
    public const string ForeignProducts = "foreign-products";
    public const string StripeWebhooks = "stripe-webhooks";
};
