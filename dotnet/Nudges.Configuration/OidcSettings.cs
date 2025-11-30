namespace Nudges.Configuration;

public class OidcSettings {
    public string ServerUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public OidcAdminCredentials? AdminCredentials { get; set; } = new OidcAdminCredentials();
}

public class OidcAdminCredentials {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
