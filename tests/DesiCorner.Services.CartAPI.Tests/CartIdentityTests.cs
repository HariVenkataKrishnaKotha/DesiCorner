using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DesiCorner.Contracts.Cart;
using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.CartAPI.Tests;

/// <summary>
/// H1: CartAPI must take the user from the validated JWT, never from a header or body
/// field the client controls.
/// </summary>
public class CartIdentityTests
{
    private static readonly Guid Victim = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Spoofed_header_without_token_does_not_select_a_users_cart()
    {
        using var factory = new CartApiFactory();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Add("X-Forwarded-UserId", Victim.ToString());

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Carts.Calls);
    }

    [Fact]
    public async Task Spoofed_header_cannot_clear_a_users_cart()
    {
        using var factory = new CartApiFactory();
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/cart/clear");
        request.Headers.Add("X-Forwarded-UserId", Victim.ToString());

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Carts.Calls);
    }

    [Fact]
    public async Task Spoofed_header_on_guest_request_keeps_the_guest_cart()
    {
        using var factory = new CartApiFactory();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Add("X-Session-Id", "guest-session");
        request.Headers.Add("X-Forwarded-UserId", Victim.ToString());

        await factory.CreateClient().SendAsync(request);

        var call = Assert.Single(factory.Carts.Calls);
        Assert.Null(call.UserId);
        Assert.Equal("guest-session", call.SessionId);
    }

    [Fact]
    public async Task Authenticated_user_gets_their_own_cart_even_with_a_spoofed_header()
    {
        using var factory = new CartApiFactory();
        var caller = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(caller));
        request.Headers.Add("X-Forwarded-UserId", Victim.ToString());

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(caller, Assert.Single(factory.Carts.Calls).UserId);
    }

    [Fact]
    public async Task Authenticated_user_is_identified_from_the_token_alone()
    {
        using var factory = new CartApiFactory();
        var caller = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(caller));

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(caller, Assert.Single(factory.Carts.Calls).UserId);
    }

    [Fact]
    public async Task UserId_in_add_to_cart_body_is_ignored()
    {
        using var factory = new CartApiFactory();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/add")
        {
            Content = JsonContent.Create(new AddToCartDto { ProductId = Guid.NewGuid(), Quantity = 1, UserId = Victim })
        };
        request.Headers.Add("X-Session-Id", "guest-session");

        await factory.CreateClient().SendAsync(request);

        var call = Assert.Single(factory.Carts.Calls);
        Assert.Null(call.UserId);
        Assert.Equal("guest-session", call.SessionId);
    }
}
