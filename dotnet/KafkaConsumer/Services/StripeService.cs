using System.Diagnostics;
using HotChocolate.Utilities;
using Microsoft.Extensions.Logging;
using Monads;
using Stripe;
using UnAd.Models;

namespace KafkaConsumer.Services;

internal class StripeService(IStripeClient stripeClient, ILogger<StripeService> logger) : IForeignProductService {
    private static readonly ActivitySource ActivitySource = new($"{typeof(StripeService).FullName}");

    private readonly ProductService _productService = new(stripeClient);
    private readonly PriceService _priceService = new(stripeClient);
    private readonly PaymentIntentService _paymentService = new(stripeClient);
    private readonly CustomerService _customerService = new(stripeClient);

    public async Task<Result<string, Exception>> CreateCustomer(string id, string phone, string name, CancellationToken cancellationToken) {
        try {
            var newCustomer = await _customerService.CreateAsync(new CustomerCreateOptions {
                Name = name,
                Phone = phone,
                Metadata = new Dictionary<string, string> {
                    { "clientId", id },
                },
            }, cancellationToken: cancellationToken);
            return newCustomer.Id;
        } catch (Exception ex) {
            logger.LogCustomerCreationFailed(ex);
            return ex;
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

    public async Task<Result<string, Exception>> DeleteProduct(string priceTierId, CancellationToken cancellationToken) {
        try {
            var existing = await _productService.DeleteAsync(priceTierId, cancellationToken: cancellationToken);
            return existing.Id;
        } catch (StripeException e) {
            logger.LogPriceTierDeleteFailed(priceTierId, e);
            return e;
        }
    }

    public async Task<Result<bool, Exception>> VerifyPayment(string paymentIntentId, CancellationToken cancellationToken) {
        try {
            var existing = await _paymentService.GetAsync(paymentIntentId, cancellationToken: cancellationToken);
            return string.IsNullOrEmpty(existing.Id);
        } catch (StripeException e) {
            logger.LogException(e);
            return e;
        }
    }

    public async Task<Result<string, PriceTierCreationError>> CreateForeignPrice(IGetPriceTier_PriceTier tier, CancellationToken cancellationToken) {
        using var activity = ActivitySource.StartActivity(nameof(CreateForeignPrice), ActivityKind.Internal, Activity.Current?.Context ?? default);
        activity?.SetTag("priceTierId", tier.Id);
        activity?.Start();
        try {
            var price = await _priceService.CreateAsync(tier.ToShopifyPriceCreateOptions(), cancellationToken: cancellationToken);
            if (price is not null) {
                activity?.SetTag("foreignServiceId", price.Id);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return price.Id;
            } else {
                return new PriceTierCreationError("No product returned");
            }
        } catch (StripeException ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new PriceTierCreationError(ex.Message, ex);
        }
    }

    public async Task<Result<string, ProductCreationError>> CreateForeignProduct(IGetPlan_Plan plan, CancellationToken cancellationToken) {
        using var activity = ActivitySource.StartActivity(nameof(CreateForeignProduct), ActivityKind.Client, Activity.Current?.Context ?? default);
        activity?.SetTag("planId", plan.Id);
        activity?.Start();
        try {
            var product = await _productService.CreateAsync(plan.ToShopifyProductCreateOptions(), new RequestOptions {
                IdempotencyKey = Activity.Current?.Id,
            }, cancellationToken);
            if (product is null) {
                activity?.SetStatus(ActivityStatusCode.Error, "No product returned");
                return new ProductCreationError("No product returned");
            } else {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return product.Id;
            }
        } catch (StripeException ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new ProductCreationError(ex.Message, ex);
        }
    }

    public async Task<Result<bool, PriceTierUpdateError>> UpdateForeignPrice(IGetPriceTier_PriceTier tier, CancellationToken cancellationToken) {
        using var activity = ActivitySource.StartActivity(nameof(UpdateForeignPrice), ActivityKind.Client, Activity.Current?.Context ?? default);
        activity?.SetTag("priceTierId", tier.Id);
        activity?.Start();
        try {
            var price = await _priceService.UpdateAsync(tier.ForeignServiceId, tier.ToShopifyPriceUpdateOptions(), cancellationToken: cancellationToken);
            if (price is null) {
                return new PriceTierUpdateError("No product returned");
            } else {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return true;
            }
        } catch (StripeException ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return new PriceTierUpdateError(ex.Message, ex);
        }
    }

    public async Task<Result<bool, ProductUpdateError>> UpdateForeignProduct(IGetPlan_Plan plan, CancellationToken cancellationToken) {
        try {
            var product = await _productService.UpdateAsync(plan.ForeignServiceId, plan.ToShopifyProductUpdateOptions(), new RequestOptions {
                IdempotencyKey = Activity.Current?.Id,
            }, cancellationToken);
            if (product is null) {
                return new ProductUpdateError("No product returned");
            } else {
                return true;
            }
        } catch (StripeException ex) {
            return new ProductUpdateError(ex.Message, ex);
        }
    }

    public async Task<Result<bool, ProductDeleteError>> DeleteForeignProduct(string id, CancellationToken cancellationToken) {
        try {
            var product = await _productService.DeleteAsync(id, cancellationToken: cancellationToken);
            return product is null
                ? new ProductDeleteError("No product returned")
                : true;
        } catch (StripeException ex) {
            return ex.HttpStatusCode is System.Net.HttpStatusCode.NotFound
                ? true
                : new ProductDeleteError(ex.Message, ex);
        }
    }

    public async Task<Result<bool, PriceTierDeleteError>> DeleteForeignPrice(string id, CancellationToken cancellationToken) {
        // https://github.com/stripe/stripe-python/issues/658#issuecomment-634106645
        try {
            var price = await _priceService.UpdateAsync(id, new PriceUpdateOptions {
                Active = false,
            }, new RequestOptions(), cancellationToken);
            return price is null
                ? new PriceTierDeleteError("No price returned")
                : true;
        } catch (StripeException e) {
            return e.HttpStatusCode is System.Net.HttpStatusCode.NotFound
                ? true
                : new PriceTierDeleteError(e.Message, e);
        }
    }
}

internal static partial class StripeProductServiceLogs {
    [LoggerMessage(Level = LogLevel.Error, Message = "Delete of product {ProductId} failed")]
    public static partial void LogProductDeleteFailed(this ILogger<StripeService> logger, string productId, Exception exception);
    [LoggerMessage(Level = LogLevel.Error, Message = "An exception has occurred.")]
    public static partial void LogException(this ILogger<StripeService> logger, Exception exception);
    [LoggerMessage(Level = LogLevel.Error, Message = "Delete of price tier {PriceTierId} failed")]
    public static partial void LogPriceTierDeleteFailed(this ILogger<StripeService> logger, string priceTierId, Exception exception);
    [LoggerMessage(Level = LogLevel.Error, Message = "Lookup of price tier {PriceTierId} failed")]
    public static partial void LogPriceTierLookupFailed(this ILogger<StripeService> logger, string priceTierId, Exception exception);
    [LoggerMessage(Level = LogLevel.Error, Message = "Customer creation failed.")]
    public static partial void LogCustomerCreationFailed(this ILogger<StripeService> logger, Exception exception);
}

internal static class ProductMappings {
    public static ProductCreateOptions ToShopifyProductCreateOptions(this IGetPlan_Plan plan) =>
        new() {
            Name = plan.Name,
            Description = string.IsNullOrEmpty(plan.Description) ? null : plan.Description,
            Active = plan.IsActive,
            Images = string.IsNullOrEmpty(plan.IconUrl) ? default : [plan.IconUrl],
            Type = "service",
            Metadata = new Dictionary<string, string> {
                        { "planId", plan.Id },
                    },
            MarketingFeatures = [
                new ProductMarketingFeatureOptions {
                    Name = $"{plan.Features.MaxMessages} messages per billing period",
                },
                new ProductMarketingFeatureOptions {
                    Name = $"{plan.Features.SupportTier} support",
                },
                new ProductMarketingFeatureOptions {
                    Name = $"{(plan.Features.AiSupport == true ? string.Empty : "No ")}AI Features",
                }
            ],
        };

    public static ProductUpdateOptions ToShopifyProductUpdateOptions(this IGetPlan_Plan plan) =>
        new() {
            Name = plan.Name,
            Description = string.IsNullOrEmpty(plan.Description) ? null : plan.Description,
            Active = plan.IsActive,
            Images = string.IsNullOrEmpty(plan.IconUrl) ? default : [plan.IconUrl],
            Metadata = new Dictionary<string, string> {
                { "planId", plan.Id },
            },
            MarketingFeatures = [
                new ProductMarketingFeatureOptions {
                    Name = $"{plan.Features.MaxMessages} messages per billing period",
                },
                new ProductMarketingFeatureOptions {
                    Name = $"{plan.Features.SupportTier} support",
                },
                new ProductMarketingFeatureOptions {
                    Name = $"{(plan.Features.AiSupport == true ? string.Empty : "No ")}AI Features",
                }
            ],
        };

    public static PriceCreateOptions ToShopifyPriceCreateOptions(this IGetPriceTier_PriceTier tier) =>
        new() {
            Active = true,
            Product = tier.Plan?.ForeignServiceId!,
            Metadata = new Dictionary<string, string> {
                { "priceTierId", tier.Id },
            },
            Recurring = new PriceRecurringOptions {
                Interval = GetIntervalString(tier.Duration),
            },
            LookupKey = tier.Id,
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

    public static PriceUpdateOptions ToShopifyPriceUpdateOptions(this IGetPriceTier_PriceTier tier) =>
        new() {
            Active = Enum.GetName(tier.Status)?.ToUpperInvariant().EqualsInvariantIgnoreCase(
                PriceTierStatusExtensions.ToStatusString(BasicStatus.Active)),
        };
}
