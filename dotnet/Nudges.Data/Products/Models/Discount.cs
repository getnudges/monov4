using System;
using System.Collections.Generic;

namespace Nudges.Data.Products.Models;

public partial class Discount
{
    public int Id { get; set; }

    public int? DiscountCodeId { get; set; }

    public Guid? PlanSubscriptionId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual DiscountCode? DiscountCode { get; set; }

    public virtual PlanSubscription? PlanSubscription { get; set; }
}
