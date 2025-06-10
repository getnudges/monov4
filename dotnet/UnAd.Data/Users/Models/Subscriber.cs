using System;
using System.Collections.Generic;

namespace UnAd.Data.Users.Models;

public partial class Subscriber
{
    public string PhoneNumber { get; set; } = null!;

    public DateTime? JoinedDate { get; set; }

    public string Locale { get; set; } = null!;

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}
