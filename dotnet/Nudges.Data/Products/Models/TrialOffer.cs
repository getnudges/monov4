using System;
using System.Collections.Generic;

namespace Nudges.Data.Products.Models;

public partial class TrialOffer
{
    public int Id { get; set; }

    public int? PriceTierId { get; set; }

    public TimeSpan Duration { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual PriceTier? PriceTier { get; set; }

    public virtual ICollection<Trial> Trials { get; set; } = new List<Trial>();
}
