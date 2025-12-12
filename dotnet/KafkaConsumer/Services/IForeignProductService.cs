using Monads;
using Stripe;

namespace KafkaConsumer.Services;

internal interface IForeignProductService {
    public Task<string> CreateCustomer(Guid id, string phone, string name, CancellationToken cancellationToken);
    public Task<Maybe<string>> GetPriceIdByLookupId(string priceTierId, CancellationToken cancellationToken);
    public Task<Product> CreateForeignProduct(ProductCreateOptions plan, CancellationToken cancellationToken);
    public Task UpdateForeignProduct(Nudges.Contracts.Products.Plan plan, CancellationToken cancellationToken);
    public Task DeleteForeignProduct(string id, CancellationToken cancellationToken);
    public Task<string> CreateForeignPrice(Nudges.Contracts.Products.PriceTier tier, CancellationToken cancellationToken);
    public Task UpdateForeignPrice(Nudges.Contracts.Products.PriceTier tier, CancellationToken cancellationToken);
    public Task DeleteForeignPrice(string id, CancellationToken cancellationToken);
    public Task<bool> VerifyPayment(string paymentIntentId, CancellationToken cancellationToken);
}
