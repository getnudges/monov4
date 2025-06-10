namespace UnAd.Stripe;

public interface IRateLimiter {
    bool ShouldAllowRequest();
}
