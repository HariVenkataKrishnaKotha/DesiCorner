using System.Net;
using System.Net.Http.Json;
using DesiCorner.Contracts.Orders;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// Item 4: a guest checkout whose email/phone matches a registered account is linked to that
/// account. The lookup must use the "AuthAPI" client that Program registers. OrderService
/// used to ask for "AuthServer", which has no base address, so the lookup always failed.
/// </summary>
public class GuestAccountLinkingTests
{
    [Fact]
    public async Task Guest_checkout_looks_up_the_account_on_the_AuthServer_and_links_the_order()
    {
        var registered = Guid.NewGuid();
        using var factory = new OrderApiFactory { RegisteredUserId = registered };

        var response = await factory.CreateClient().PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            Email = "registered@example.test", Phone = "5550102", SessionId = "session-s1",
            OrderType = "Pickup", PaymentMethod = "PayAtPickup"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lookup = Assert.Single(factory.Downstream.Requests, r => r.Uri.AbsolutePath == "/api/account/user-lookup");
        Assert.Equal("https://localhost:7001", lookup.Uri.GetLeftPart(UriPartial.Authority));
        Assert.Equal("?email=registered%40example.test&phone=5550102", lookup.Uri.Query);

        using var db = factory.CreateDbContext();
        var order = Assert.Single(db.Orders);
        Assert.Equal(registered, order.UserId);
        Assert.False(order.IsGuestOrder);
    }

    [Fact]
    public async Task Guest_checkout_with_no_matching_account_stays_a_guest_order()
    {
        using var factory = new OrderApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            Email = "new@example.test", Phone = "5550103", SessionId = "session-s1",
            OrderType = "Pickup", PaymentMethod = "PayAtPickup"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(factory.Downstream.Requests, r => r.Uri.AbsolutePath == "/api/account/user-lookup");
        using var db = factory.CreateDbContext();
        var order = Assert.Single(db.Orders);
        Assert.Null(order.UserId);
        Assert.True(order.IsGuestOrder);
    }
}
