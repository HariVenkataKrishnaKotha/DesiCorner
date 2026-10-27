using System.Net;

namespace DesiCorner.Gateway.Tests;

public class GatewaySmokeTests : IClassFixture<GatewayFactory>
{
    private readonly GatewayFactory _factory;

    public GatewaySmokeTests(GatewayFactory factory) => _factory = factory;

    [Fact]
    public async Task Proxies_cart_requests_to_the_cart_service()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(_factory.Upstream.Requests, r => r.Uri.Port == 7301 && r.Uri.AbsolutePath == "/api/cart/");
    }
}
