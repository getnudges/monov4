using Monads;
using Stripe;

namespace KafkaConsumer.Services;

internal interface IForeignProductService {
    public Task<Result<string, Exception>> CreateCustomer(Guid id, string phone, string name, CancellationToken cancellationToken);
    public Task<Maybe<string>> GetPriceIdByLookupId(string priceTierId, CancellationToken cancellationToken);
    public Task<Result<string, ProductCreationError>> CreateForeignProduct(ProductCreateOptions plan, CancellationToken cancellationToken);
    public Task<Result<bool, ProductUpdateError>> UpdateForeignProduct(Nudges.Contracts.Products.Plan plan, CancellationToken cancellationToken);
    public Task<Result<bool, ProductDeleteError>> DeleteForeignProduct(string id, CancellationToken cancellationToken);
    public Task<Result<string, PriceTierCreationError>> CreateForeignPrice(Nudges.Contracts.Products.PriceTier tier, CancellationToken cancellationToken);
    public Task<Result<bool, PriceTierUpdateError>> UpdateForeignPrice(Nudges.Contracts.Products.PriceTier tier, CancellationToken cancellationToken);
    public Task<Result<bool, PriceTierDeleteError>> DeleteForeignPrice(string id, CancellationToken cancellationToken);
    public Task<Result<bool, Exception>> VerifyPayment(string paymentIntentId, CancellationToken cancellationToken);
}

internal record ProductCreationError(string Message, Exception? Exception = null);
internal record PriceTierCreationError(string Message, Exception? Exception = null);
internal record ProductUpdateError(string Message, Exception? Exception = null);
internal record ProductDeleteError(string Message, Exception? Exception = null);
internal record PriceTierUpdateError(string Message, Exception? Exception = null);
internal record PriceTierDeleteError(string Message, Exception? Exception = null);
