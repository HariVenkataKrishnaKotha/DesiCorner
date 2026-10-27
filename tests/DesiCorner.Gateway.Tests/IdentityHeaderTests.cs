using System.Net;

namespace DesiCorner.Gateway.Tests;

/// <summary>
/// H1: downstream services trust X-Forwarded-* identity headers, so the gateway must be
/// the only party able to set them. Whatever a client sends must never reach a service.
/// </summary>
public class IdentityHeaderTests
{
    private static readonly Guid Victim = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static TheoryData<string, string> SpoofedHeaders => new()
    {
        { "X-Forwarded-UserId", Victim.ToString() },
        { "X-Forwarded-Roles", "Admin" },
        { "X-Forwarded-Email", "victim@example.test" },
        { "X-Forwarded-Phone", "5550199" },
        { "X-Auth-Source", "jwt" },
        { "x-forwarded-userid", Victim.ToString() }
    };

    [Theory]
    [MemberData(nameof(SpoofedHeaders))]
    public async Task Anonymous_request_cannot_smuggle_identity_headers(string header, string value)
    {
        using var factory = new GatewayFactory();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart/");
        request.Headers.Add("X-Session-Id", "guest-session");
        request.Headers.TryAddWithoutValidation(header, value);

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forwarded = Assert.Single(factory.Upstream.Requests);
        Assert.Null(forwarded.Header(header));
        Assert.Equal("guest-session", forwarded.Header("X-Session-Id"));
    }

    [Theory]
    [MemberData(nameof(SpoofedHeaders))]
    public async Task Public_route_cannot_smuggle_identity_headers(string header, string value)
    {
        using var factory = new GatewayFactory();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/");
        request.Headers.TryAddWithoutValidation(header, value);

        await factory.CreateClient().SendAsync(request);

        var forwarded = Assert.Single(factory.Upstream.Requests);
        Assert.Null(forwarded.Header(header));
    }

    [Fact]
    public async Task Invalid_token_on_optional_auth_route_cannot_smuggle_identity()
    {
        using var factory = new GatewayFactory();
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/cart/clear");
        request.Headers.Add("Authorization", "Bearer not-a-valid-token");
        request.Headers.Add("X-Forwarded-UserId", Victim.ToString());

        await factory.CreateClient().SendAsync(request);

        var forwarded = Assert.Single(factory.Upstream.Requests);
        Assert.Null(forwarded.Header("X-Forwarded-UserId"));
    }

    [Fact]
    public async Task Authenticated_request_forwards_only_the_token_identity()
    {
        using var factory = new GatewayFactory();
        var caller = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart/");
        request.Headers.Add("Authorization", "Bearer " + FakeTokenAuthenticator.TokenFor(caller));
        request.Headers.Add("X-Forwarded-UserId", Victim.ToString());
        request.Headers.Add("X-Forwarded-Email", "victim@example.test");

        await factory.CreateClient().SendAsync(request);

        var forwarded = Assert.Single(factory.Upstream.Requests);
        Assert.Equal(caller.ToString(), forwarded.Header("X-Forwarded-UserId"));
        Assert.Equal($"{caller}@example.test", forwarded.Header("X-Forwarded-Email"));
    }
}
