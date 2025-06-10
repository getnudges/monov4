using Stripe;

namespace Nudges.Webhooks.Stripe;

public record StripeEventContext(Event StripeEvent, HttpRequest Request);
