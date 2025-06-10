using System;
using System.Collections.Generic;

namespace Nudges.Data.Products.Models;

public partial class Trial
{
    public int Id { get; set; }

    public int? TrailOfferId { get; set; }

    public Guid? PlanSubscriptionId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual PlanSubscription? PlanSubscription { get; set; }

    public virtual TrialOffer? TrailOffer { get; set; }
}
