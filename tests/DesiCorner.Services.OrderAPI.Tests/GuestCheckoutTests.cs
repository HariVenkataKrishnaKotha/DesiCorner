using System.Net;
using System.Net.Http.Json;
using DesiCorner.Contracts.Orders;

namespace DesiCorner.Services.OrderAPI.Tests;

public class GuestCheckoutTests
{
    [Fact]
    public async Task Guest_pickup_order_uses_the_session_cart()
    {
        using var factory = new OrderApiFactory();
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString();

        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            Email = "guest@example.test",
            Phone = "5550100",
            SessionId = sessionId,
            OrderType = "Pickup",
            PaymentMethod = "PayAtPickup"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cartRequest = Assert.Single(factory.Downstream.Requests, r => r.Method == HttpMethod.Get && r.Uri.AbsolutePath == "/api/cart");
        Assert.Equal(sessionId, cartRequest.Header("X-Session-Id"));
        Assert.Null(cartRequest.Header("Authorization"));

        using var db = factory.CreateDbContext();
        var order = Assert.Single(db.Orders);
        Assert.True(order.IsGuestOrder);
        Assert.Equal(100.00m, order.Total);
    }
}
