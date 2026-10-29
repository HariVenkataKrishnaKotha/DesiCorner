using System.Net;
using DesiCorner.Tests.Shared;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// AuthServer accepted OpenIddict requests over plain HTTP and allowed HTTP JWT metadata in every
/// environment. Both relaxations are now Development-only.
/// </summary>
public class TlsValidationTests
{
    private static HttpClient OverHttp(AuthServerFactory factory) =>
        factory.CreateClient(new() { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task Outside_Development_OpenIddict_refuses_plain_http()
    {
        using var factory = new AuthServerFactory("Production", ServiceSecrets.Both(out _));

        var response = await OverHttp(factory).GetAsync("/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("HTTPS", await response.Content.ReadAsStringAsync());
        Assert.True(TlsInspection.RequiresHttpsMetadata(factory.Services));
    }

    [Fact]
    public async Task Development_still_accepts_plain_http()
    {
        using var factory = new AuthServerFactory();

        var response = await OverHttp(factory).GetAsync("/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(TlsInspection.RequiresHttpsMetadata(factory.Services));
    }
}
