using System;
using System.Collections.Generic;

namespace Nudges.Data.Users.Models;

public partial class Announcement
{
    public string MessageSid { get; set; } = null!;

    public DateTime? SentOn { get; set; }

    public Guid? ClientId { get; set; }

    public virtual Client? Client { get; set; }
}
