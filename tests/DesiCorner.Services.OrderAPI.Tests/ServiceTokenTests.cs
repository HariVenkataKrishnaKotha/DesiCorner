using System.Net;
using System.Net.Http.Json;
using DesiCorner.Contracts.Orders;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// The AuthServer user lookup now requires a client-credentials token with the
/// desicorner.internal.user-lookup scope. OrderAPI obtains one from AuthServer's token endpoint,
/// caches it until shortly before it expires, and attaches it to the "AuthAPI" client. If no
/// token can be had, checkout still succeeds and the order stays a guest order.
/// </summary>
public class ServiceTokenTests
{
    private static int _phone = 5550200;

    private static async Task<HttpResponseMessage> GuestCheckout(OrderApiFactory factory) =>
        await factory.CreateClient().PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            Email = "registered@example.test", Phone = Interlocked.Increment(ref _phone).ToString(),
            SessionId = "session-s1", OrderType = "Pickup", PaymentMethod = "PayAtPickup"
        });

    private static Dictionary<string, string> Form(string? body) =>
        (body ?? "").Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(kv => Uri.UnescapeDataString(kv[0]), kv => Uri.UnescapeDataString(kv[1].Replace('+', ' ')));

    [Fact]
    public async Task The_lookup_carries_a_client_credentials_token_for_the_lookup_scope()
    {
        var registered = Guid.NewGuid();
        using var factory = new OrderApiFactory { RegisteredUserId = registered };

        var response = await GuestCheckout(factory);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokenRequest = Assert.Single(factory.TokenRequests);
        Assert.Equal("https://localhost:7001", tokenRequest.Uri.GetLeftPart(UriPartial.Authority));
        var form = Form(tokenRequest.Body);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("desicorner-order-service", form["client_id"]);
        Assert.Equal(OrderApiFactory.TestServiceSecret, form["client_secret"]);
        Assert.Equal("desicorner.internal.user-lookup", form["scope"]);

        var lookup = Assert.Single(factory.LookupRequests);
        Assert.Equal("Bearer service-token-1", lookup.Header("Authorization"));
        using var db = factory.CreateDbContext();
        Assert.Equal(registered, Assert.Single(db.Orders).UserId);
    }

    [Fact]
    public async Task The_token_is_reused_until_shortly_before_it_expires()
    {
        using var factory = new OrderApiFactory { RegisteredUserId = Guid.NewGuid(), TokenLifetimeSeconds = 3600 };

        await GuestCheckout(factory);
        factory.Clock.Advance(TimeSpan.FromMinutes(30));
        await GuestCheckout(factory);

        Assert.Single(factory.TokenRequests);
        Assert.All(factory.LookupRequests, r => Assert.Equal("Bearer service-token-1", r.Header("Authorization")));
    }

    [Fact]
    public async Task A_token_about_to_expire_is_replaced()
    {
        using var factory = new OrderApiFactory { RegisteredUserId = Guid.NewGuid(), TokenLifetimeSeconds = 3600 };

        await GuestCheckout(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(3600 - 10));
        await GuestCheckout(factory);

        Assert.Equal(2, factory.TokenRequests.Count);
        Assert.Equal("Bearer service-token-2", factory.LookupRequests[^1].Header("Authorization"));
    }

    [Fact]
    public async Task When_no_token_can_be_obtained_checkout_succeeds_as_a_guest_order()
    {
        using var factory = new OrderApiFactory { RegisteredUserId = Guid.NewGuid(), TokenStatus = HttpStatusCode.BadRequest };

        var response = await GuestCheckout(factory);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(factory.TokenRequests);
        using var db = factory.CreateDbContext();
        var order = Assert.Single(db.Orders);
        Assert.Null(order.UserId);
        Assert.True(order.IsGuestOrder);
    }

    [Fact]
    public void Outside_Development_a_missing_secret_stops_startup()
    {
        using var factory = new OrderApiFactory { EnvironmentName = "Production", ServiceSecret = null };

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(OrderApiFactory.ServiceSecretKey, error.ToString());
    }

    [Fact]
    public async Task In_Development_a_missing_secret_skips_linking_and_checkout_still_succeeds()
    {
        using var factory = new OrderApiFactory { EnvironmentName = "Development", ServiceSecret = null, RegisteredUserId = Guid.NewGuid() };

        var response = await GuestCheckout(factory);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(factory.TokenRequests);
        // Without a secret OrderAPI cannot be authorized, so it does not call the lookup at all
        Assert.Empty(factory.LookupRequests);
        using var db = factory.CreateDbContext();
        Assert.True(Assert.Single(db.Orders).IsGuestOrder);
    }
}
