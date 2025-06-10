using System.Security.Claims;

namespace ProductApi.Tests.Integration;

public static class TestServices {
    public static IQueryRequestBuilder AddUser(this IQueryRequestBuilder builder, IEnumerable<Claim> claims) =>
        builder.SetUser(new ClaimsPrincipal(new ClaimsIdentity(claims)));

}



