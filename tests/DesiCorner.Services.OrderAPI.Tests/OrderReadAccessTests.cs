using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DesiCorner.Contracts.Orders;
using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// Item 2: GET /api/orders/{id} returns an order only to its owner. That means the user who
/// placed it, an admin, or (for guest orders) the X-Session-Id that placed it. Anyone else
/// gets the same 404 as for an unknown id.
/// </summary>
public class OrderReadAccessTests
{
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private const string SessionS1 = "session-s1";
    private const string SessionS2 = "session-s2";

    private sealed record Reader(string Name, Guid? UserId = null, string? SessionId = null, bool Admin = false)
    {
        public override string ToString() => Name;
    }

    private static HttpClient ClientFor(OrderApiFactory factory, Reader reader)
    {
        var client = factory.CreateClient();
        if (reader.UserId is { } userId)
        {
            var token = reader.Admin
                ? TestJwt.CreateToken(userId, "admin@example.test", null, "Admin")
                : TestJwt.CreateToken(userId, $"{userId}@example.test", "5550101");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (reader.SessionId != null)
        {
            client.DefaultRequestHeaders.Add("X-Session-Id", reader.SessionId);
        }
        return client;
    }

    private static async Task<Guid> PlaceGuestOrder(OrderApiFactory factory, string sessionId)
    {
        var client = ClientFor(factory, new Reader("placer", SessionId: sessionId));
        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            Email = "guest@example.test", Phone = "5550100", SessionId = sessionId,
            OrderType = "Pickup", PaymentMethod = "PayAtPickup"
        });
        return await OrderId(response);
    }

    private static async Task<Guid> PlaceUserOrder(OrderApiFactory factory, Guid userId)
    {
        var client = ClientFor(factory, new Reader("placer", userId));
        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderDto { OrderType = "Pickup", PaymentMethod = "PayAtPickup" });
        return await OrderId(response);
    }

    private static async Task<Guid> OrderId(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("result").GetProperty("id").GetGuid();
    }

    private static async Task<(HttpStatusCode Status, string Body)> Read(OrderApiFactory factory, Reader reader, Guid orderId)
    {
        var response = await ClientFor(factory, reader).GetAsync($"/api/orders/{orderId}");
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Guest_order_is_readable_only_with_the_session_that_placed_it()
    {
        using var factory = new OrderApiFactory();
        var orderId = await PlaceGuestOrder(factory, SessionS1);
        var unknown = await Read(factory, new Reader("nobody", SessionId: SessionS1), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.OK, (await Read(factory, new Reader("same guest", SessionId: SessionS1), orderId)).Status);

        foreach (var other in new[]
        {
            new Reader("other guest", SessionId: SessionS2),
            new Reader("anonymous"),
            new Reader("signed-in user", UserA),
        })
        {
            var result = await Read(factory, other, orderId);
            Assert.True(result == unknown, $"{other}: expected the unknown-id 404, got {(int)result.Status} {result.Body}");
        }
    }

    [Fact]
    public async Task User_order_is_readable_only_by_that_user_or_an_admin()
    {
        using var factory = new OrderApiFactory();
        var orderId = await PlaceUserOrder(factory, UserA);
        var unknown = await Read(factory, new Reader("nobody", UserB), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.OK, (await Read(factory, new Reader("owner", UserA), orderId)).Status);
        Assert.Equal(HttpStatusCode.OK, (await Read(factory, new Reader("admin", Guid.NewGuid(), Admin: true), orderId)).Status);

        foreach (var other in new[]
        {
            new Reader("other user", UserB),
            new Reader("guest", SessionId: SessionS1),
            new Reader("anonymous"),
        })
        {
            var result = await Read(factory, other, orderId);
            Assert.True(result == unknown, $"{other}: expected the unknown-id 404, got {(int)result.Status} {result.Body}");
        }
    }

    [Fact]
    public async Task Admin_can_read_a_guest_order()
    {
        using var factory = new OrderApiFactory();
        var orderId = await PlaceGuestOrder(factory, SessionS1);

        Assert.Equal(HttpStatusCode.OK, (await Read(factory, new Reader("admin", Guid.NewGuid(), Admin: true), orderId)).Status);
    }

    [Fact]
    public async Task Unknown_order_is_a_404()
    {
        using var factory = new OrderApiFactory();

        Assert.Equal(HttpStatusCode.NotFound, (await Read(factory, new Reader("guest", SessionId: SessionS1), Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task Guest_order_linked_to_a_registered_account_is_readable_by_the_guest_session_and_the_account()
    {
        var registered = Guid.NewGuid();
        using var factory = new OrderApiFactory { RegisteredUserId = registered };
        var orderId = await PlaceGuestOrder(factory, SessionS1);

        Assert.Equal(HttpStatusCode.OK, (await Read(factory, new Reader("same guest", SessionId: SessionS1), orderId)).Status);
        Assert.Equal(HttpStatusCode.OK, (await Read(factory, new Reader("account", registered), orderId)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Read(factory, new Reader("other guest", SessionId: SessionS2), orderId)).Status);
    }
}
