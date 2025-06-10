namespace Nudges.Stripe;

public interface IRateLimiter {
    bool ShouldAllowRequest();
}
