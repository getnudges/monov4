using Nudges.Configuration;

namespace Nudges.AuthInit;

internal class Settings {
    public OidcSettings Oidc { get; set; } = new();
}
