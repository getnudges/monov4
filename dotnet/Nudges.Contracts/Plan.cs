namespace Nudges.Contracts.Products;

/// <summary>
/// Integration contract representing a product Plan.
/// Immutable and serialization-friendly. Does not expose EF navigation types.
/// </summary>
public sealed record Plan(
    int Id,
    string Name,
    string? Description,
    string? IconUrl,
    bool? IsActive,
    string? ForeignServiceId,
    PlanFeature? Features,
    IReadOnlyList<PriceTier> PriceTiers);

/// <summary>
/// Feature limits for a Plan.
/// </summary>
public sealed record PlanFeature(
    int? MaxMessages,
    string? SupportTier,
    bool? AiSupport);

/// <summary>
/// Pricing tier belonging to a Plan.
/// </summary>
public sealed record PriceTier(
    int Id,
    int PlanId,
    decimal Price,
    TimeSpan Duration,
    string Name,
    string? Description,
    string? IconUrl,
    string? ForeignServiceId,
    string Status,
    IReadOnlyList<DiscountCode> DiscountCodes,
    IReadOnlyList<TrialOffer> TrialOffers);


/// <summary>
/// Discount code definition.
/// </summary>
public sealed record DiscountCode(
    int Id,
    int? PriceTierId,
    string Code,
    decimal Discount,
    TimeSpan? Duration,
    string Name,
    string? Description,
    DateTime? ExpiryDate);

/// <summary>
/// Applied discount instance.
/// </summary>
public sealed record Discount(
    int Id,
    int? DiscountCodeId,
    Guid? PlanSubscriptionId);

/// <summary>
/// Trial offer definition.
/// </summary>
public sealed record TrialOffer(
    int Id,
    int? PriceTierId,
    TimeSpan Duration,
    string Name,
    string? Description,
    DateTime? ExpiryDate);

/// <summary>
/// Trial instance attached to a subscription.
/// </summary>
public sealed record Trial(
    int Id,
    int? TrialOfferId,
    Guid? PlanSubscriptionId);

/// <summary>
/// Subscription to a Plan via a PriceTier.
/// </summary>
public sealed record PlanSubscription(
    Guid Id,
    int? PriceTierId,
    DateTime StartDate,
    DateTime EndDate,
    string? Status,
    Guid ClientId,
    Guid PaymentConfirmationId);
