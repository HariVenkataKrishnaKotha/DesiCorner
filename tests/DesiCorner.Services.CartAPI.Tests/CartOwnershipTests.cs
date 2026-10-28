using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesiCorner.Contracts.Cart;

namespace DesiCorner.Services.CartAPI.Tests;

/// <summary>
/// Item 1 (IDOR): every endpoint that takes a cart or cart-item id must act only inside the
/// caller's own cart, and an id from someone else's cart must look exactly like an unknown id.
/// </summary>
public class CartOwnershipTests
{
    /// <summary>Owner and intruder pairs: two users, two guests, and both mixes.</summary>
    public static TheoryData<Caller, Caller> Pairs => new()
    {
        { Caller.User("user A"), Caller.User("user B") },
        { Caller.Guest("guest S1"), Caller.Guest("guest S2") },
        { Caller.User("user A"), Caller.Guest("guest S1") },
        { Caller.Guest("guest S1"), Caller.User("user A") },
    };

    private sealed record CartView(Guid CartId, Guid ItemId, int Quantity, string? Coupon, int ItemCount);

    private static async Task<CartView> Read(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var cart = json.RootElement.GetProperty("result");
        var items = cart.GetProperty("items");
        var first = items.GetArrayLength() > 0 ? items[0] : default;
        return new CartView(
            cart.GetProperty("id").GetGuid(),
            first.ValueKind == JsonValueKind.Undefined ? Guid.Empty : first.GetProperty("id").GetGuid(),
            first.ValueKind == JsonValueKind.Undefined ? 0 : first.GetProperty("quantity").GetInt32(),
            cart.GetProperty("couponCode").GetString(),
            items.GetArrayLength());
    }

    private static async Task<CartView> AddItem(HttpClient client, Guid productId, int quantity = 2) =>
        await Read(await client.PostAsJsonAsync("/api/cart/add", new AddToCartDto { ProductId = productId, Quantity = quantity }));

    private static async Task<CartView> GetCart(HttpClient client) => await Read(await client.GetAsync("/api/cart"));

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Update_cannot_change_an_item_in_someone_elses_cart(Caller owner, Caller intruder)
    {
        using var factory = new OwnedCartApiFactory();
        var ownerClient = factory.ClientFor(owner);
        var intruderClient = factory.ClientFor(intruder);
        var victim = await AddItem(ownerClient, OwnedCartApiFactory.Samosa, quantity: 2);
        await AddItem(intruderClient, OwnedCartApiFactory.Chai);

        var response = await intruderClient.PutAsJsonAsync("/api/cart/update",
            new UpdateCartItemDto { CartItemId = victim.ItemId, Quantity = 9 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(2, (await GetCart(ownerClient)).Quantity);
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Remove_cannot_delete_an_item_from_someone_elses_cart(Caller owner, Caller intruder)
    {
        using var factory = new OwnedCartApiFactory();
        var ownerClient = factory.ClientFor(owner);
        var intruderClient = factory.ClientFor(intruder);
        var victim = await AddItem(ownerClient, OwnedCartApiFactory.Samosa);
        await AddItem(intruderClient, OwnedCartApiFactory.Chai);

        var response = await intruderClient.DeleteAsync($"/api/cart/item/{victim.ItemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, (await GetCart(ownerClient)).ItemCount);
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Apply_coupon_cannot_target_someone_elses_cart(Caller owner, Caller intruder)
    {
        using var factory = new OwnedCartApiFactory();
        var ownerClient = factory.ClientFor(owner);
        var intruderClient = factory.ClientFor(intruder);
        var victim = await AddItem(ownerClient, OwnedCartApiFactory.Samosa);
        await AddItem(intruderClient, OwnedCartApiFactory.Chai);

        var response = await intruderClient.PostAsJsonAsync("/api/cart/apply-coupon",
            new ApplyCouponDto { CartId = victim.CartId, CouponCode = OwnedCartApiFactory.ValidCoupon });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null((await GetCart(ownerClient)).Coupon);
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Remove_coupon_cannot_target_someone_elses_cart(Caller owner, Caller intruder)
    {
        using var factory = new OwnedCartApiFactory();
        var ownerClient = factory.ClientFor(owner);
        var intruderClient = factory.ClientFor(intruder);
        var victim = await AddItem(ownerClient, OwnedCartApiFactory.Samosa);
        (await ownerClient.PostAsJsonAsync("/api/cart/apply-coupon",
            new ApplyCouponDto { CartId = victim.CartId, CouponCode = OwnedCartApiFactory.ValidCoupon })).EnsureSuccessStatusCode();
        await AddItem(intruderClient, OwnedCartApiFactory.Chai);

        var response = await intruderClient.PostAsync($"/api/cart/remove-coupon/{victim.CartId}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(OwnedCartApiFactory.ValidCoupon, (await GetCart(ownerClient)).Coupon);
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Someone_elses_ids_look_exactly_like_unknown_ids(Caller owner, Caller intruder)
    {
        using var factory = new OwnedCartApiFactory();
        var victim = await AddItem(factory.ClientFor(owner), OwnedCartApiFactory.Samosa);
        var intruderClient = factory.ClientFor(intruder);
        await AddItem(intruderClient, OwnedCartApiFactory.Chai);
        var unknown = Guid.NewGuid();

        async Task<(HttpStatusCode, string)> Probe(Func<Guid, Guid, Task<HttpResponseMessage>> call, Guid itemId, Guid cartId)
        {
            var response = await call(itemId, cartId);
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        var calls = new Func<Guid, Guid, Task<HttpResponseMessage>>[]
        {
            (item, _) => intruderClient.PutAsJsonAsync("/api/cart/update", new UpdateCartItemDto { CartItemId = item, Quantity = 1 }),
            (item, _) => intruderClient.DeleteAsync($"/api/cart/item/{item}"),
            (_, cart) => intruderClient.PostAsJsonAsync("/api/cart/apply-coupon", new ApplyCouponDto { CartId = cart, CouponCode = OwnedCartApiFactory.ValidCoupon }),
            (_, cart) => intruderClient.PostAsync($"/api/cart/remove-coupon/{cart}", null),
        };

        foreach (var call in calls)
        {
            var foreign = await Probe(call, victim.ItemId, victim.CartId);
            var missing = await Probe(call, unknown, unknown);
            Assert.Equal(HttpStatusCode.NotFound, missing.Item1);
            Assert.Equal(missing, foreign);
        }
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Owner_can_still_manage_their_own_cart(Caller owner, Caller other)
    {
        using var factory = new OwnedCartApiFactory();
        var client = factory.ClientFor(owner);
        await AddItem(factory.ClientFor(other), OwnedCartApiFactory.Chai);
        var mine = await AddItem(client, OwnedCartApiFactory.Samosa, quantity: 1);
        await AddItem(client, OwnedCartApiFactory.Chai);

        var updated = await Read(await client.PutAsJsonAsync("/api/cart/update", new UpdateCartItemDto { CartItemId = mine.ItemId, Quantity = 4 }));
        Assert.Equal(4, updated.Quantity);

        var withCoupon = await Read(await client.PostAsJsonAsync("/api/cart/apply-coupon",
            new ApplyCouponDto { CartId = mine.CartId, CouponCode = OwnedCartApiFactory.ValidCoupon }));
        Assert.Equal(OwnedCartApiFactory.ValidCoupon, withCoupon.Coupon);

        var withoutCoupon = await Read(await client.PostAsync($"/api/cart/remove-coupon/{mine.CartId}", null));
        Assert.Null(withoutCoupon.Coupon);

        var afterRemove = await Read(await client.DeleteAsync($"/api/cart/item/{mine.ItemId}"));
        Assert.Equal(1, afterRemove.ItemCount);
    }

    [Fact]
    public async Task Invalid_coupon_on_own_cart_is_still_a_400()
    {
        using var factory = new OwnedCartApiFactory();
        var client = factory.ClientFor(Caller.User("user A"));
        var mine = await AddItem(client, OwnedCartApiFactory.Samosa);

        var response = await client.PostAsJsonAsync("/api/cart/apply-coupon", new ApplyCouponDto { CartId = mine.CartId, CouponCode = "BOGUS" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
