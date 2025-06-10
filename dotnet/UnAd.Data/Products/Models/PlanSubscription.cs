using System;
using System.Collections.Generic;

namespace UnAd.Data.Products.Models;

public partial class PlanSubscription
{
    public Guid Id { get; set; }

    public int? PriceTierId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Status { get; set; }

    public Guid ClientId { get; set; }

    public Guid PaymentConfirmationId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    public virtual PriceTier? PriceTier { get; set; }

    public virtual ICollection<Trial> Trials { get; set; } = new List<Trial>();
}
