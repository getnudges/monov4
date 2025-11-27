namespace Nudges.Data.Users.Models;

/// <summary>
/// Represents a customer of Nudges.
/// </summary>
public partial class Client {
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? CustomerId { get; set; }

    public string? SubscriptionId { get; set; }

    public virtual User IdNavigation { get; set; } = null!;

    public virtual ICollection<Subscriber> Subscribers { get; set; } = new List<Subscriber>();
}
