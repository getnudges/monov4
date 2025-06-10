using Stripe;
using Stripe.Checkout;

namespace PaymentApi.Services;

public class StripePaymentProvider(IStripeClient stripeClient) : IPaymentProvider {
    private readonly SessionService _sessionService = new(stripeClient);
    private readonly CustomerService _customerService = new(stripeClient);

    public int MerchantServiceId => 1;

    public async Task CancelCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken) =>
        await _sessionService.ExpireAsync(sessionId, cancellationToken: cancellationToken);

    public async Task<string> CreateCustomer(string clientNodeId, CancellationToken cancellationToken) {

        var options = new CustomerCreateOptions {
            Metadata = new Dictionary<string, string> {
                { "clientNodeId", clientNodeId },
            },
        };
        var customer = await _customerService.CreateAsync(options, cancellationToken: cancellationToken);
        return customer.Id;
    }

    public Task CompleteCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken) => throw new NotImplementedException();

    public async Task<ProviderCheckoutSession> CreateCheckoutSessionAsync(string priceForeignServiceId, string customerId, Uri successUrl, Uri cancelUrl, CancellationToken cancellationToken) {
        var options = new SessionCreateOptions {
            PaymentMethodTypes = ["card"],
            Mode = "subscription",
            LineItems = [
                new() {
                    Price = priceForeignServiceId,
                    Quantity = 1
                },
            ],
            Customer = customerId,
            SuccessUrl = $"{successUrl}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = cancelUrl.ToString(),
        };
        try {
            var session = await _sessionService.CreateAsync(options, null, cancellationToken);
            return new ProviderCheckoutSession(
                session.Id,
                priceForeignServiceId,
                customerId,
                new Uri(session.Url),
                new Uri(session.SuccessUrl),
                new Uri(session.CancelUrl),
                session.ExpiresAt);
        } catch (StripeException e) {
            throw new CheckoutSessionException(e.Message);
        }
    }

    public async Task<Monads.Result<ProviderPaymentVerification, string>> VerifyPaymentAsync(string sessionId, CancellationToken cancellationToken) {
        var result = await _sessionService.GetAsync(sessionId, cancellationToken: cancellationToken);
        if (result.PaymentIntent.Status != "succeeded") {
            return "Payment not successful";
        }
        return new ProviderPaymentVerification(
            result.Subscription.Id,
            result.PaymentIntent.Id,
            new ProviderCheckoutSession(
                result.Id,
                result.LineItems.Data[0].Price.Id,
                result.ClientReferenceId,
                new Uri(result.Url),
                new Uri(result.SuccessUrl),
                new Uri(result.CancelUrl),
                result.ExpiresAt)
            );
    }
}
