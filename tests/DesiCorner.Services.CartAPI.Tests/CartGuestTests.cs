using System.Net;

namespace DesiCorner.Services.CartAPI.Tests;

public class CartGuestTests
{
    [Fact]
    public async Task Guest_cart_is_resolved_from_the_session_id_header()
    {
        using var factory = new CartApiFactory();
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Add("X-Session-Id", sessionId);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var call = Assert.Single(factory.Carts.Calls);
        Assert.Null(call.UserId);
        Assert.Equal(sessionId, call.SessionId);
    }

    [Fact]
    public async Task Request_without_token_or_session_is_rejected()
    {
        using var factory = new CartApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Carts.Calls);
    }
}
