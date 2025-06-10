using System;
using System.Collections.Generic;

namespace UnAd.Data.Products.Models;

public partial class PriceTier
{
    public int Id { get; set; }

    public int? PlanId { get; set; }

    public decimal Price { get; set; }

    public TimeSpan Duration { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ForeignServiceId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<DiscountCode> DiscountCodes { get; set; } = new List<DiscountCode>();

    public virtual Plan? Plan { get; set; }

    public virtual ICollection<PlanSubscription> PlanSubscriptions { get; set; } = new List<PlanSubscription>();

    public virtual ICollection<TrialOffer> TrialOffers { get; set; } = new List<TrialOffer>();
}
