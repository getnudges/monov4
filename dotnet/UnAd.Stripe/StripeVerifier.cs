using Microsoft.Extensions.Logging;
using Stripe;

namespace UnAd.Stripe;

public interface IStripeVerifier {
    bool TryVerify(string stripeSignature, string stripeEndpointSecret, string json, out Event @event);
}

public class StripeVerifier(ILogger<StripeVerifier> logger) : IStripeVerifier {
    public bool TryVerify(string stripeSignature, string stripeEndpointSecret, string json, out Event @event) {
        ArgumentException.ThrowIfNullOrEmpty(stripeSignature, nameof(stripeSignature));
        ArgumentException.ThrowIfNullOrEmpty(json, nameof(json));

        try {
            @event = EventUtility.ConstructEvent(json, stripeSignature, stripeEndpointSecret);
            return @event is not null;
        } catch (StripeException e) {
            logger.LogStripeVerificationFailure(e);
            @event = default!;
            return false;
        }
    }
}
