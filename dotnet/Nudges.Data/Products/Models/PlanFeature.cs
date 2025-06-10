using System;
using System.Collections.Generic;

namespace Nudges.Data.Products.Models;

public partial class PlanFeature
{
    public int PlanId { get; set; }

    public int? MaxMessages { get; set; }

    public string? SupportTier { get; set; }

    public bool? AiSupport { get; set; }

    public virtual Plan Plan { get; set; } = null!;
}
