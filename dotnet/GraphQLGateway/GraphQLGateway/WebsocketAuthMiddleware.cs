using Precision.WarpCache.Grpc.Client;

namespace GraphQLGateway;

public class WebSocketAuthMiddleware(RequestDelegate next, ICacheClient<string> cache) {
    public async Task InvokeAsync(HttpContext context) {
        if (!context.WebSockets.IsWebSocketRequest) {
            await next(context);
            return;
        }
        if (!context.Request.Cookies.TryGetValue("TokenId", out var cookieValue) || string.IsNullOrEmpty(cookieValue)) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var tokenId = await cache.GetAsync($"token:{cookieValue}");
        context.Request.Headers.Append("Authorization", $"Bearer {tokenId}");
        await next(context);

    }
}

