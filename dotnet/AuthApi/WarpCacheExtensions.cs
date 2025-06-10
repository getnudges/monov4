using Precision.WarpCache.Grpc.Client;

namespace AuthApi;

public static class WarpCacheExtensions {
    private static class Keys {
        public static string OtpSecret(string phoneNumber) => $"otp:{phoneNumber}:secret";
        public static string TokenId(string tokenId) => $"token:{tokenId}";
        public static string OidcState(string state) => $"oidc:{state}";
    }

    public static readonly TimeSpan OtpExpirationTime = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan CodeVerifierExpirationTime = TimeSpan.FromMinutes(5);
    public static Task SetOtpSecret(this ICacheClient<string> cache, string phoneNumber, string secret) =>
        cache.SetAsync(Keys.OtpSecret(phoneNumber), secret, OtpExpirationTime);

    public static Task<string?> GetOtpSecret(this ICacheClient<string> cache, string phoneNumber) =>
        cache.GetAsync(Keys.OtpSecret(phoneNumber));

    public static Task RemoveOtpSecret(this ICacheClient<string> cache, string phoneNumber) =>
        cache.RemoveAsync(Keys.OtpSecret(phoneNumber));

    public static Task SetToken(this ICacheClient<string> cache, string tokenId, string token, int expiresIn) =>
        cache.SetAsync(Keys.TokenId(tokenId), token, TimeSpan.FromSeconds(expiresIn));

    public static Task<string?> GetToken(this ICacheClient<string> cache, string tokenId) =>
        cache.GetAsync(Keys.TokenId(tokenId));

    public static Task SetOidcState(this ICacheClient<string> cache, string state, string codeVerifier) =>
        cache.SetAsync(Keys.OidcState(state), codeVerifier, CodeVerifierExpirationTime);

    public static Task<string?> GetOidcState(this ICacheClient<string> cache, string state) =>
        cache.GetAsync(Keys.OidcState(state));

    public static Task RemoveOidcState(this ICacheClient<string> cache, string state) =>
        cache.RemoveAsync(Keys.OidcState(state));

}
