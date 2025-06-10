using System;
using System.Collections.Generic;

namespace UnAd.Data.Products.Models;

public partial class PriceTierFeature
{
    public int PriceTierId { get; set; }

    public int? MaxMessages { get; set; }

    public string? SupportTier { get; set; }

    public bool? AiSupport { get; set; }

    public virtual PriceTier PriceTier { get; set; } = null!;
}
