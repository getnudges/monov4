using System.Text.Json.Serialization;

namespace Nudges.Auth;

public class UserRepresentation {
    public string? Id { get; set; }

    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public bool? EmailVerified { get; set; }

    public Dictionary<string, ICollection<string>>? Attributes { get; set; }

    public bool? Enabled { get; set; }

    public List<CredentialRepresentation>? Credentials { get; set; }

    public List<string>? RequiredActions { get; set; }

    public List<string>? Groups { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}

public class CredentialRepresentation {
    public string? Type { get; set; }

    public string? Value { get; set; }

    public bool? Temporary { get; set; }
}
