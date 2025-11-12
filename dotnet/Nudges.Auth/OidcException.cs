namespace Nudges.Auth;

public class OidcException : Exception {
    public AuthApiError? ApiError { get; init; }

    public OidcException(string message) : base(message) { }

    private OidcException(string message, AuthApiError? apiError = null, Exception? inner = null)
        : base(message, inner) => ApiError = apiError;

    public static OidcException FromApiError(AuthApiError error) =>
        new(error.ErrorDescription ?? error.Error ?? "Unknown OIDC API error", error);

    public static OidcException FromException(string message, Exception ex) =>
        new(message, inner: ex);
}
