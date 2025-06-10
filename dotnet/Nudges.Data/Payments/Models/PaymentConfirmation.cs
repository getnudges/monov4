using System;
using System.Collections.Generic;

namespace Nudges.Data.Payments.Models;

public partial class PaymentConfirmation
{
    public Guid Id { get; set; }

    public int? MerchantServiceId { get; set; }

    public string ConfirmationCode { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual MerchantService? MerchantService { get; set; }
}
