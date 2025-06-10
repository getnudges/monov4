using System;
using System.Collections.Generic;

namespace Nudges.Data.Payments.Models;

public partial class MerchantService
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<PaymentConfirmation> PaymentConfirmations { get; set; } = new List<PaymentConfirmation>();
}
