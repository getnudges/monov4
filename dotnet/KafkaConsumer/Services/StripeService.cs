using System.Diagnostics;
using System.Globalization;
using HotChocolate.Utilities;
using Monads;
using Nudges.Kafka.Events;
using Nudges.Models;
using Stripe;

namespace KafkaConsumer.Services;

internal class StripeService(IStripeClient stripeClient, ILogger<StripeService> logger) : IForeignProductService {
    private static readonly ActivitySource ActivitySource = new($"{typeof(StripeService).FullName}");

    private readonly ProductService _productService = new(stripeClient);
    private readonly PriceService _priceService = new(stripeClient);
    private readonly PaymentIntentService _paymentService = new(stripeClient);
    private readonly CustomerService _customerService = new(stripeClient);

    public async Task<string> CreateCustomer(Guid id, string phone, string name, CancellationToken cancellationToken) {
        try {
            var newCustomer = await _customerService.CreateAsync(new CustomerCreateOptions {
                Name = name,
                Phone = phone,
                Metadata = new Dictionary<string, string> {
                    { "clientId", id.ToString("N") },
                },
            }, cancellationToken: cancellationToken);
            return newCustomer.Id;
        } catch (Exception ex) {
            logger.LogCustomerCreationFailed(ex);
            throw new ProductCreationException("Failed to create customer", ex);
        }
    }

    public async Task<Maybe<string>> GetPriceIdByLookupId(string priceTierId, CancellationToken cancellationToken) {
        try {
            var existing = await _priceService.ListAsync(new PriceListOptions {
                Limit = 1,
                LookupKeys = [priceTierId],
            }, cancellationToken: cancellationToken);
            return existing.FirstOrDefault() is Price price
                ? price.Id
                : Maybe<string>.None;
        } catch (Exception e) {
            logger.LogPriceTierLookupFailed(priceTierId, e);
            return Maybe<string>.None;
        }
    }

    public async Task<string> CreateForeignProduct(ProductCreateOptions plan, CancellationToken cancellationToken) {
        using var activity = ActivitySource.StartActivity(nameof(CreateForeignProduct), ActivityKind.Client, Activity.Current?.Context ?? default);
        try {
            var sw = Stopwatch.StartNew();
            var product = await _productService.CreateAsync(plan, new RequestOptions {
                IdempotencyKey = Activity.Current?.Id,
            }, cancellationToken);
            sw.Stop();
            activity?.SetTag("product.creationDurationMs", sw.ElapsedMilliseconds);
            if (product is null) {
                activity?.SetStatus(ActivityStatusCode.Error, "No product returned");
                logger.LogNoProductReturned();
                throw new ProductCreationException("No product returned from Stripe");
            } else {
                activity?.SetTag("product.id", product.Id);
                activity?.SetStatus(ActivityStatusCode.Ok);
                logger.LogForeignProductCreated(product.Id);
                return product.Id;
            }
        } catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            logger.LogProductCreatedException(ex);
            throw new ProductCreationException(ex.Message, ex);
        }
    }

    public async Task<string> CreateForeignPrice(Nudges.Contracts.Products.PriceTier tier, CancellationToken cancellationToken) {
        using var activity = ActivitySource.StartActivity(nameof(CreateForeignPrice), ActivityKind.Internal, Activity.Current?.Context ?? default);
        activity?.SetTag("priceTierId", tier.Id);
        activity?.Start();
        try {
            var price = await _priceService.CreateAsync(tier.ToShopifyPriceCreateOptions(""), cancellationToken: cancellationToken);
            if (price is not null) {
                activity?.SetTag("foreignServiceId", price.Id);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return price.Id;
            } else {
                throw new PriceTierCreationException("No price returned from Stripe");
            }
        } catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw new PriceTierCreationException(ex.Message, ex);
        }
    }

    public async Task UpdateForeignPrice(Nudges.Contracts.Products.PriceTier tier, CancellationToken cancellationToken) {
        using var activity = ActivitySource.StartActivity(nameof(UpdateForeignPrice), ActivityKind.Client, Activity.Current?.Context ?? default);
        activity?.SetTag("priceTierId", tier.Id);

        try {
            var price = await _priceService.UpdateAsync(tier.ForeignServiceId, tier.ToShopifyPriceUpdateOptions(), cancellationToken: cancellationToken);
            if (price is null) {
                throw new PriceTierUpdateException("No price returned from Stripe");
            } else {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return;
            }
        } catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw new PriceTierUpdateException(ex.Message, ex);
        }
    }

    public async Task UpdateForeignProduct(Nudges.Contracts.Products.Plan plan, CancellationToken cancellationToken) {
        try {
            var product = await _productService.UpdateAsync(plan.ForeignServiceId, plan.ToShopifyProductUpdateOptions(), new RequestOptions {
                IdempotencyKey = Activity.Current?.Id,
            }, cancellationToken);
            if (product is null) {
                throw new ProductUpdateException("No product returned from Stripe");
            } else {
                return;
            }
        } catch (StripeException ex) {
            throw new ProductUpdateException(ex.Message, ex);
        }
    }

    public async Task DeleteForeignProduct(string id, CancellationToken cancellationToken) {
        try {
            var product = await _productService.DeleteAsync(id, cancellationToken: cancellationToken);
            if (product is null) {
                throw new ProductDeleteException("No product returned from Stripe");
            }
            return;
        } catch (StripeException ex) {
            if (ex.HttpStatusCode is System.Net.HttpStatusCode.NotFound) {
                return;
            }
            throw new ProductDeleteException(ex.Message, ex);
        }
    }

    public async Task DeleteForeignPrice(string id, CancellationToken cancellationToken) {
        try {
            var price = await _priceService.UpdateAsync(id, new PriceUpdateOptions {
                Active = false,
            }, new RequestOptions(), cancellationToken);
            if (price is null) {
                throw new PriceTierDeleteException("No price returned from Stripe");
            }
            return;
        } catch (StripeException e) {
            if (e.HttpStatusCode is System.Net.HttpStatusCode.NotFound) {
                return;
            }
            throw new PriceTierDeleteException(e.Message, e);
        }
    }

    public async Task<bool> VerifyPayment(string paymentIntentId, CancellationToken cancellationToken) {
        try {
            var existing = await _paymentService.GetAsync(paymentIntentId, cancellationToken: cancellationToken);
            return string.IsNullOrEmpty(existing.Id);
        } catch (Exception e) {
            logger.LogException(e);
            throw;
        }
    }
}

internal static partial class StripeProductServiceLogs {
    [LoggerMessage(Level = LogLevel.Error, Message = "Delete of product {ProductId} failed")]
    public static partial void LogProductDeleteFailed(this ILogger<StripeService> logger, string productId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "An exception has occurred.")]
    public static partial void LogException(this ILogger<StripeService> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "An exception has occurred while creating a foreign product.")]
    public static partial void LogProductCreatedException(this ILogger<StripeService> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "No Product returned from Stripe")]
    public static partial void LogNoProductReturned(this ILogger<StripeService> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Foreign Product with ID {ProductId} created")]
    public static partial void LogForeignProductCreated(this ILogger<StripeService> logger, string productId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Delete of price tier {PriceTierId} failed")]
    public static partial void LogPriceTierDeleteFailed(this ILogger<StripeService> logger, string priceTierId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Lookup of price tier {PriceTierId} failed")]
    public static partial void LogPriceTierLookupFailed(this ILogger<StripeService> logger, string priceTierId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Customer creation failed.")]
    public static partial void LogCustomerCreationFailed(this ILogger<StripeService> logger, Exception exception);
}

internal static class ProductMappings {
    public static ProductCreateOptions ToShopifyProductCreateOptions(this PlanCreatedEvent data) =>
        new() {
            Name = data.Plan.Name,
            Description = string.IsNullOrEmpty(data.Plan.Description) ? null : data.Plan.Description,
            //Active = plan.IsActive,
            Images = string.IsNullOrEmpty(data.Plan.IconUrl) ? default : [data.Plan.IconUrl],
            Type = "service",
            Metadata = new Dictionary<string, string> {
                        { "planId", data.Plan.Id.ToString(CultureInfo.InvariantCulture) },
                    },
            //MarketingFeatures = [
            //    new ProductMarketingFeatureOptions {
            //        Name = $"{plan.Features.MaxMessages} messages per billing period",
            //    },
            //    new ProductMarketingFeatureOptions {
            //        Name = $"{plan.Features.SupportTier} support",
            //    },
            //    new ProductMarketingFeatureOptions {
            //        Name = $"{(plan.Features.AiSupport == true ? string.Empty : "No ")}AI Features",
            //    }
            //],
        };

    public static ProductUpdateOptions ToShopifyProductUpdateOptions(this Nudges.Contracts.Products.Plan plan) =>
        new() {
            Name = plan.Name,
            Description = string.IsNullOrEmpty(plan.Description) ? null : plan.Description,
            Active = plan.IsActive,
            Images = string.IsNullOrEmpty(plan.IconUrl) ? default : [plan.IconUrl],
            Metadata = new Dictionary<string, string> {
                { "planId", plan.Id.ToString(CultureInfo.InvariantCulture) },
            },
            MarketingFeatures = plan.Features is not null ? [
                new ProductMarketingFeatureOptions {
                    Name = $"{plan.Features.MaxMessages} messages per billing period",
                },
                new ProductMarketingFeatureOptions {
                    Name = $"{plan.Features.SupportTier} support",
                },
                new ProductMarketingFeatureOptions {
                    Name = $"{(plan.Features.AiSupport == true ? string.Empty : "No ")}AI Features",
                }
            ] : [],
        };

    public static PriceCreateOptions ToShopifyPriceCreateOptions(this Nudges.Contracts.Products.PriceTier tier, string productId) =>
        new() {
            Active = true,
            Product = productId,
            Metadata = new Dictionary<string, string> {
                { "priceTierId", tier.Id.ToString(CultureInfo.InvariantCulture) },
            },
            Recurring = new PriceRecurringOptions {
                //Interval = GetIntervalString(tier.Duration),
            },
            //LookupKey = tier.Id.ToString(CultureInfo.InvariantCulture),
            Currency = "usd",
            UnitAmount = (long)Math.Round(tier.Price * 100)
        };

    public static string GetIntervalString(BasicDuration duration) =>
        duration switch {
            BasicDuration.P30d => "month",
            BasicDuration.P365d => "year",
            BasicDuration.P7d => "week",
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, "Invalid duration"),
        };

    public static PriceUpdateOptions ToShopifyPriceUpdateOptions(this Nudges.Contracts.Products.PriceTier tier) =>
        new() {
            Active = tier.Status.ToUpperInvariant().EqualsInvariantIgnoreCase(
                PriceTierStatusExtensions.ToStatusString(BasicStatus.Active)),
        };
}
