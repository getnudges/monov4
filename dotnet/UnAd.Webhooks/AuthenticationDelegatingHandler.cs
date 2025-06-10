using System.Net.Http.Headers;
using Precision.WarpCache;
using UnAd.Auth;

namespace UnAd.Webhooks;

internal sealed class AuthenticationDelegatingHandler(IServerTokenClient authService,
                                                      ChannelCacheMediator<string, string> channelCache,
                                                      IHttpContextAccessor httpContextAccessor,
                                                      IConfiguration configuration) : DelegatingHandler {

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {

        var context = httpContextAccessor.HttpContext;
        if (context is not null && context.Items["traceparent"] is string traceParent) {
            request.Headers.Add("traceparent", traceParent);
        }
        if (!request.Headers.Contains("Authorization")) {
            // TODO: need to add caching of this token
            var tokenId = await GetToken();

            if (tokenId is not null) {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetToken() {
        var cached = await channelCache.GetAsync("token");
        if (cached.Value is not null) {
            return cached.Value;
        }
        var tokens = await authService.GetTokenAsync("webhooks");

        // TODO: this is kinda ugly, tbh
        var tokenId = tokens.Match<string>(
            success => success.AccessToken,
            failure => string.Empty);

        await channelCache.SetAsync("token", tokenId);
        return tokenId;
    }
}
