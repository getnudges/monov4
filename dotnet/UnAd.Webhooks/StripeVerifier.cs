using Monads;
using Stripe;

namespace UnAd.Webhooks;

public interface IStripeVerifier {
    public Result<Event, Exception> Verify(string stripeSignature, string stripeEndpointSecret, string json);
}

public class StripeVerifier(ILogger<StripeVerifier> logger) : IStripeVerifier {

    public Result<Event, Exception> Verify(string stripeSignature, string stripeEndpointSecret, string json) {
        if (string.IsNullOrEmpty(stripeEndpointSecret)) {
            return new ArgumentException($"'{nameof(stripeEndpointSecret)}' cannot be null or empty.", nameof(stripeEndpointSecret));
        }

        if (string.IsNullOrEmpty(json)) {
            return new ArgumentException($"'{nameof(json)}' cannot be null or empty.", nameof(json));
        }

        try {
            return EventUtility.ConstructEvent(json, stripeSignature, stripeEndpointSecret);
        } catch (StripeException ex) {
            logger.LogStripeVerificationFailure(ex);
            return ex;
        }
    }
}
