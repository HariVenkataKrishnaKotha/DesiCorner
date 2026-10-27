using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DesiCorner.Contracts.Orders;
using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// H1: OrderAPI reaches CartAPI through the gateway, which now strips X-Forwarded-UserId.
/// It must act on the cart as the caller (by forwarding their bearer token, or the guest
/// session id), never by naming a user in a header.
/// </summary>
public class OrderIdentityTests
{
    private static CreateOrderDto PickupOrder(string? sessionId = null, string? email = null, string? phone = null) => new()
    {
        Email = email,
        Phone = phone,
        SessionId = sessionId,
        OrderType = "Pickup",
        PaymentMethod = "PayAtPickup"
    };

    [Fact]
    public async Task Authenticated_checkout_forwards_the_callers_token_to_CartAPI()
    {
        using var factory = new OrderApiFactory();
        var caller = Guid.NewGuid();
        var token = TestJwt.CreateToken(caller, "caller@example.test", "5550101");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/orders", PickupOrder());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cartCalls = factory.Downstream.Requests.Where(r => r.Uri.AbsolutePath.StartsWith("/api/cart")).ToList();
        Assert.Equal(2, cartCalls.Count); // fetch, then clear
        Assert.All(cartCalls, r =>
        {
            Assert.Equal("Bearer " + token, r.Header("Authorization"));
            Assert.Null(r.Header("X-Forwarded-UserId"));
        });

        using var db = factory.CreateDbContext();
        Assert.Equal(caller, Assert.Single(db.Orders).UserId);
    }

    [Fact]
    public async Task Authenticated_checkout_takes_contact_details_from_the_token()
    {
        using var factory = new OrderApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestJwt.CreateToken(Guid.NewGuid(), "caller@example.test", "5550101"));
        client.DefaultRequestHeaders.Add("X-Forwarded-Email", "victim@example.test");
        client.DefaultRequestHeaders.Add("X-Forwarded-Phone", "5550199");

        var response = await client.PostAsJsonAsync("/api/orders", PickupOrder());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var db = factory.CreateDbContext();
        var order = Assert.Single(db.Orders);
        Assert.Equal("caller@example.test", order.UserEmail);
        Assert.Equal("5550101", order.UserPhone);
    }

    [Fact]
    public async Task Guest_matching_a_registered_user_checks_out_the_session_cart()
    {
        using var factory = new OrderApiFactory { RegisteredUserId = Guid.NewGuid() };
        var sessionId = Guid.NewGuid().ToString();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/orders", PickupOrder(sessionId, "registered@example.test", "5550102"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cartCalls = factory.Downstream.Requests.Where(r => r.Uri.AbsolutePath.StartsWith("/api/cart")).ToList();
        Assert.NotEmpty(cartCalls);
        Assert.All(cartCalls, r =>
        {
            Assert.Equal(sessionId, r.Header("X-Session-Id"));
            Assert.Null(r.Header("X-Forwarded-UserId"));
        });

        // The order is still linked to the registered account, as before.
        using var db = factory.CreateDbContext();
        Assert.Equal(factory.RegisteredUserId, Assert.Single(db.Orders).UserId);
    }
}
