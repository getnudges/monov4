namespace Nudges.Data.Users.Models;

public partial class Subscriber {
    public Guid Id { get; set; }

    public virtual User IdNavigation { get; set; } = null!;

    /// <summary>
    /// Many-to-many: subscribers belong to many clients.
    /// This will soon map to a join table with SubscriberId instead of phone number.
    /// </summary>
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}
