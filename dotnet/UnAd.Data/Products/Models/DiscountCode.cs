using System;
using System.Collections.Generic;

namespace UnAd.Data.Products.Models;

public partial class DiscountCode
{
    public int Id { get; set; }

    public int? PriceTierId { get; set; }

    public string Code { get; set; } = null!;

    public decimal Discount { get; set; }

    public TimeSpan? Duration { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    public virtual PriceTier? PriceTier { get; set; }
}
