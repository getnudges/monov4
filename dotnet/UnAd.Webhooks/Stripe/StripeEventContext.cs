using Stripe;

namespace UnAd.Webhooks.Stripe;

public record StripeEventContext(Event StripeEvent, HttpRequest Request);
