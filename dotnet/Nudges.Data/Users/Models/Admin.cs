namespace Nudges.Data.Users.Models;

public partial class Admin {
    public Guid Id { get; set; }

    public virtual User IdNavigation { get; set; } = null!;
}
