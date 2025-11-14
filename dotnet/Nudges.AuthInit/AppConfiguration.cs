using Nudges.Configuration;

namespace Nudges.AuthInit;

internal static class AppConfiguration {
    [ConfigurationKey]
    public const string UserDbConnectionString = "ConnectionStrings:UserDb";
    [ConfigurationKey]
    public const string OidcServerUrl = "Oidc:ServerUrl";
}
