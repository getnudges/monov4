namespace Nudges.Data.Comms.Models;

public partial class Announcement {
    public string MessageSid { get; set; } = null!;

    public DateTime? SentOn { get; set; }

    public Guid? ClientId { get; set; }
}
