using Monads;

namespace PaymentApi.Services;

public interface IPaymentProvider {
    public int MerchantServiceId { get; }
    public Task<string> CreateCustomer(string clientNodeId, CancellationToken cancellationToken);
    public Task<ProviderCheckoutSession> CreateCheckoutSessionAsync(string priceForeignServiceId, string customerId, Uri successUrl, Uri cancelUrl, CancellationToken cancellationToken);
    public Task<Result<ProviderPaymentVerification, string>> VerifyPaymentAsync(string sessionId, CancellationToken cancellationToken);
    public Task CancelCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken);
}

public class CheckoutSessionException(string message) : Exception(message);

public record ProviderCheckoutSession(string Id, string PriceForeignServiceId, string ClientNodeId, Uri CheckoutUrl, Uri SuccessUrl, Uri CancelUrl, DateTimeOffset Expiration);

public record ProviderPaymentVerification(string SubscriptionId, string ConfirmationCode, ProviderCheckoutSession OriginalSession);
