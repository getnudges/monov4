namespace Nudges.Contracts;

public sealed record Client(
    Guid Id,
    string NodeId,
    string Name,
    string Slug,
    string? CustomerId = null,
    string? SubscriptionId = null);
