namespace Nudges.Auth;

public class OidcConfig {
    public OidcConfig(string realm, string clientId, string clientSecret, OidcCredentials adminCredentials) {
        Realm = realm;
        ClientId = clientId;
        ClientSecret = clientSecret;
        AdminCredentials = adminCredentials;
    }

    public OidcConfig() { }
    public string Realm { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public OidcCredentials AdminCredentials { get; set; }
}

public class OidcCredentials {
    public OidcCredentials(string username, string password, string adminClientId = "admin-cli") {
        Username = username;
        Password = password;
        AdminClientId = adminClientId;
    }

    public OidcCredentials() { }

    public string Username { get; set; }
    public string Password { get; set; }
    public string AdminClientId { get; set; }
}
