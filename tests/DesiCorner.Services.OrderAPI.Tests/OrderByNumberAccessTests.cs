using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DesiCorner.Contracts.Orders;
using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// GET /api/orders/number/{orderNumber} follows the rule GET /api/orders/{id} has had since #40:
/// the owner and admins read the order, and anyone else gets exactly the same 404 as for a
/// number that doesn't exist, so the response doesn't reveal that the order exists.
/// </summary>
public class OrderByNumberAccessTests
{
    private static HttpClient ClientFor(OrderApiFactory factory, Guid userId, bool admin = false)
    {
        var client = factory.CreateClient();
        var token = admin
            ? TestJwt.CreateToken(userId, "admin@example.test", null, "Admin")
            : TestJwt.CreateToken(userId, $"{userId}@example.test", "5550101");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> PlaceOrder(OrderApiFactory factory, Guid userId)
    {
        var response = await ClientFor(factory, userId).PostAsJsonAsync("/api/orders",
            new CreateOrderDto { OrderType = "Pickup", PaymentMethod = "PayAtPickup" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("result").GetProperty("orderNumber").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, string Body)> Read(HttpClient client, string orderNumber)
    {
        var response = await client.GetAsync($"/api/orders/number/{orderNumber}");
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_owner_and_an_admin_can_read_the_order()
    {
        using var factory = new OrderApiFactory();
        var owner = Guid.NewGuid();
        var orderNumber = await PlaceOrder(factory, owner);

        Assert.Equal(HttpStatusCode.OK, (await Read(ClientFor(factory, owner), orderNumber)).Status);
        Assert.Equal(HttpStatusCode.OK, (await Read(ClientFor(factory, Guid.NewGuid(), admin: true), orderNumber)).Status);
    }

    [Fact]
    public async Task Another_user_gets_the_same_404_as_for_an_unknown_number()
    {
        using var factory = new OrderApiFactory();
        var orderNumber = await PlaceOrder(factory, Guid.NewGuid());
        var other = ClientFor(factory, Guid.NewGuid());

        var foreign = await Read(other, orderNumber);
        var unknown = await Read(other, "DC-0000000000-0000");

        Assert.Equal(HttpStatusCode.NotFound, foreign.Status);
        Assert.Equal(unknown, foreign);
    }
}
