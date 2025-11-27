using Nudges.Security;

namespace Nudges.Data.Users.Models;

public partial class User {

    public Guid Id { get; set; }

    public string Locale { get; set; } = null!;

    public string? Subject { get; set; } = null!;

    /// <summary>
    /// The plaintext logical property. Never stored directly.
    /// Encrypted/decrypted automatically by EF interceptors.
    /// </summary>
    [Encrypted(nameof(PhoneNumberEncrypted))]
    [Hashed(nameof(PhoneNumberHash))]
    public string PhoneNumber { get; set; } = null!;

    /// <summary>
    /// The irreversible lookup value used for matching and uniqueness.
    /// This is stored directly in the DB.
    /// </summary>
    public string PhoneNumberHash { get; set; } = null!;

    /// <summary>
    /// The encrypted-at-rest value. Stored as Base64 or byte[].
    /// This is what PhoneNumber maps to at the DB level.
    /// </summary>
    public string PhoneNumberEncrypted { get; set; } = null!;

    public DateTimeOffset JoinedDate { get; set; }

    public virtual Admin? Admin { get; set; }
    public virtual Client? Client { get; set; }
    public virtual Subscriber? Subscriber { get; set; }
}
