using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesiCorner.Contracts.Cart;

namespace DesiCorner.Services.CartAPI.Tests;

/// <summary>
/// POST /api/cart/merge: after sign-in, the guest cart (by X-Session-Id) is merged into the
/// signed-in user's cart (by JWT sub). It touches only the session the caller sends.
/// </summary>
public class CartMergeTests
{
    private sealed record Line(Guid ProductId, string Name, decimal Price, int Quantity);

    private sealed record CartView(Guid? Id, IReadOnlyList<Line> Items, string? Coupon, decimal SubTotal, decimal Discount, decimal Total)
    {
        public Line? Of(Guid productId) => Items.SingleOrDefault(i => i.ProductId == productId);
    }

    private sealed record Dropped(Guid ProductId, string ProductName, int Quantity, string Reason);

    private sealed record MergeView(CartView Cart, IReadOnlyList<Dropped> DroppedItems, string? DroppedCouponCode);

    private static CartView ParseCart(JsonElement cart) => new(
        cart.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String ? id.GetGuid() : null,
        cart.GetProperty("items").EnumerateArray().Select(i => new Line(
            i.GetProperty("productId").GetGuid(),
            i.GetProperty("productName").GetString()!,
            i.GetProperty("price").GetDecimal(),
            i.GetProperty("quantity").GetInt32())).ToList(),
        cart.GetProperty("couponCode").GetString(),
        cart.GetProperty("subTotal").GetDecimal(),
        cart.GetProperty("discountAmount").GetDecimal(),
        cart.GetProperty("total").GetDecimal());

    private static async Task<CartView> GetCart(HttpClient client)
    {
        var response = await client.GetAsync("/api/cart");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return ParseCart(json.RootElement.GetProperty("result"));
    }

    private static async Task Add(HttpClient client, Guid productId, int quantity) =>
        (await client.PostAsJsonAsync("/api/cart/add", new AddToCartDto { ProductId = productId, Quantity = quantity })).EnsureSuccessStatusCode();

    private static async Task ApplyCoupon(HttpClient client, string code)
    {
        var cart = await GetCart(client);
        (await client.PostAsJsonAsync("/api/cart/apply-coupon", new ApplyCouponDto { CartId = cart.Id!.Value, CouponCode = code })).EnsureSuccessStatusCode();
    }

    private static async Task<MergeView> Merge(HttpClient client)
    {
        var response = await client.PostAsync("/api/cart/merge", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = json.RootElement.GetProperty("result");
        return new MergeView(
            ParseCart(result.GetProperty("cart")),
            result.GetProperty("droppedItems").EnumerateArray().Select(d => new Dropped(
                d.GetProperty("productId").GetGuid(),
                d.GetProperty("productName").GetString()!,
                d.GetProperty("quantity").GetInt32(),
                d.GetProperty("reason").GetString()!)).ToList(),
            result.GetProperty("droppedCouponCode").GetString());
    }

    private static string GuestKey(string sessionId) => $"cart:session:{sessionId}";

    private static string UserKey(Guid userId) => $"cart:user:{userId}";

    [Fact]
    public async Task Merges_the_guest_cart_into_an_empty_user_cart_and_deletes_the_guest_cart()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        var guestClient = factory.ClientFor(guest);
        await Add(guestClient, OwnedCartApiFactory.Samosa, 2);
        await Add(guestClient, OwnedCartApiFactory.Chai, 1);
        var userId = Guid.NewGuid();

        var merged = await Merge(factory.ClientFor(userId, guest.SessionId!));

        Assert.Equal(2, merged.Cart.Of(OwnedCartApiFactory.Samosa)!.Quantity);
        Assert.Equal(1, merged.Cart.Of(OwnedCartApiFactory.Chai)!.Quantity);
        Assert.Empty(merged.DroppedItems);
        Assert.NotNull(await factory.Store.GetAsync(UserKey(userId)));
        Assert.Null(await factory.Store.GetAsync(GuestKey(guest.SessionId!)));
    }

    [Fact]
    public async Task A_product_in_both_carts_gets_the_summed_quantity()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Samosa, 2);
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Chai, 1);
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Samosa, 3);

        var merged = await Merge(factory.ClientFor(user.UserId!.Value, guest.SessionId!));

        Assert.Equal(2, merged.Cart.Items.Count);
        Assert.Equal(5, merged.Cart.Of(OwnedCartApiFactory.Samosa)!.Quantity);
        Assert.Equal(1, merged.Cart.Of(OwnedCartApiFactory.Chai)!.Quantity);
    }

    [Fact]
    public async Task Summed_quantities_are_capped_at_the_existing_per_item_limit_of_50()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Samosa, 30);
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Samosa, 40);

        var merged = await Merge(factory.ClientFor(user.UserId!.Value, guest.SessionId!));

        Assert.Equal(50, merged.Cart.Of(OwnedCartApiFactory.Samosa)!.Quantity);
    }

    [Fact]
    public async Task A_guest_only_product_moves_over_at_the_current_ProductAPI_price()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Chai, 2);

        // The stored guest price is not trusted: tamper with it, and change the catalog price.
        var stored = (await factory.Store.GetAsync(GuestKey(guest.SessionId!)))!;
        stored.Items[0].Price = 0.01m;
        await factory.Store.SaveAsync(GuestKey(guest.SessionId!), stored, TimeSpan.FromDays(1));
        factory.Products.SetPrice(OwnedCartApiFactory.Chai, 3.75m);

        var merged = await Merge(factory.ClientFor(Guid.NewGuid(), guest.SessionId!));

        Assert.Equal(3.75m, merged.Cart.Of(OwnedCartApiFactory.Chai)!.Price);
        Assert.Equal(7.50m, merged.Cart.SubTotal);
    }

    [Fact]
    public async Task Unavailable_and_deleted_products_are_dropped_and_reported()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        var guestClient = factory.ClientFor(guest);
        await Add(guestClient, OwnedCartApiFactory.Samosa, 2);
        await Add(guestClient, OwnedCartApiFactory.Chai, 3);
        factory.Products.SetAvailable(OwnedCartApiFactory.Chai, false);
        factory.Products.Remove(OwnedCartApiFactory.Samosa);
        var userId = Guid.NewGuid();

        var merged = await Merge(factory.ClientFor(userId, guest.SessionId!));

        Assert.Empty(merged.Cart.Items);
        Assert.Equal(2, merged.DroppedItems.Count);
        Assert.Contains(merged.DroppedItems, d => d.ProductId == OwnedCartApiFactory.Samosa && d.ProductName == "Samosa" && d.Quantity == 2 && d.Reason == "not_found");
        Assert.Contains(merged.DroppedItems, d => d.ProductId == OwnedCartApiFactory.Chai && d.ProductName == "Chai" && d.Quantity == 3 && d.Reason == "unavailable");
        Assert.Null(await factory.Store.GetAsync(GuestKey(guest.SessionId!)));
    }

    [Fact]
    public async Task Merge_aborts_and_keeps_both_carts_when_ProductAPI_cannot_be_reached()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Chai, 2);
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Samosa, 1);
        factory.Products.Unreachable = true;

        var response = await factory.ClientFor(user.UserId!.Value, guest.SessionId!).PostAsync("/api/cart/merge", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(await factory.Store.GetAsync(GuestKey(guest.SessionId!)));
        factory.Products.Unreachable = false;
        var userCart = await GetCart(factory.ClientFor(user));
        Assert.Single(userCart.Items);
        Assert.Equal(1, userCart.Of(OwnedCartApiFactory.Samosa)!.Quantity);
    }

    [Fact]
    public async Task The_user_carts_coupon_is_kept()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Chai, 1);
        await ApplyCoupon(factory.ClientFor(guest), OwnedCartApiFactory.GuestOnlyCoupon);
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Samosa, 1);
        await ApplyCoupon(factory.ClientFor(user), OwnedCartApiFactory.ValidCoupon);

        var merged = await Merge(factory.ClientFor(user.UserId!.Value, guest.SessionId!));

        Assert.Equal(OwnedCartApiFactory.ValidCoupon, merged.Cart.Coupon);
        Assert.Null(merged.DroppedCouponCode);
    }

    [Fact]
    public async Task The_guest_coupon_carries_over_when_it_is_still_valid_for_the_merged_cart()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Chai, 1);
        await ApplyCoupon(factory.ClientFor(guest), OwnedCartApiFactory.ValidCoupon);
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Samosa, 1);

        var merged = await Merge(factory.ClientFor(user.UserId!.Value, guest.SessionId!));

        Assert.Equal(OwnedCartApiFactory.ValidCoupon, merged.Cart.Coupon);
        Assert.Equal(1.00m, merged.Cart.Discount);
        Assert.Null(merged.DroppedCouponCode);
    }

    [Fact]
    public async Task The_guest_coupon_is_dropped_and_reported_when_it_fails_validation_for_the_user()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Chai, 1);
        await ApplyCoupon(factory.ClientFor(guest), OwnedCartApiFactory.GuestOnlyCoupon);

        var merged = await Merge(factory.ClientFor(Guid.NewGuid(), guest.SessionId!));

        Assert.Null(merged.Cart.Coupon);
        Assert.Equal(0m, merged.Cart.Discount);
        Assert.Equal(OwnedCartApiFactory.GuestOnlyCoupon, merged.DroppedCouponCode);
    }

    [Fact]
    public async Task A_second_merge_returns_the_user_cart_unchanged()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Samosa, 2);
        var client = factory.ClientFor(Guid.NewGuid(), guest.SessionId!);

        var first = await Merge(client);
        var second = await Merge(client);

        Assert.Equal(first.Cart, second.Cart with { Items = second.Cart.Items });
        Assert.Equal(2, second.Cart.Of(OwnedCartApiFactory.Samosa)!.Quantity);
        Assert.Empty(second.DroppedItems);
    }

    [Fact]
    public async Task A_session_with_no_cart_returns_the_user_cart_unchanged()
    {
        using var factory = new OwnedCartApiFactory();
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Samosa, 1);

        var merged = await Merge(factory.ClientFor(user.UserId!.Value, "session-without-a-cart"));

        Assert.Single(merged.Cart.Items);
        Assert.Equal(1, merged.Cart.Of(OwnedCartApiFactory.Samosa)!.Quantity);
    }

    [Fact]
    public async Task No_carts_at_all_returns_an_empty_cart()
    {
        using var factory = new OwnedCartApiFactory();

        var merged = await Merge(factory.ClientFor(Guid.NewGuid(), "session-without-a-cart"));

        Assert.Empty(merged.Cart.Items);
        Assert.Equal(0m, merged.Cart.Total);
    }

    [Fact]
    public async Task Only_the_session_the_caller_sends_is_merged()
    {
        using var factory = new OwnedCartApiFactory();
        var mine = Caller.Guest("guest S1");
        var other = Caller.Guest("guest S2");
        await Add(factory.ClientFor(mine), OwnedCartApiFactory.Samosa, 1);
        await Add(factory.ClientFor(other), OwnedCartApiFactory.Chai, 4);

        var merged = await Merge(factory.ClientFor(Guid.NewGuid(), mine.SessionId!));

        Assert.Null(merged.Cart.Of(OwnedCartApiFactory.Chai));
        var untouched = await GetCart(factory.ClientFor(other));
        Assert.Equal(4, untouched.Of(OwnedCartApiFactory.Chai)!.Quantity);
    }

    [Fact]
    public async Task Without_a_signed_in_caller_merge_is_401_and_nothing_changes()
    {
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Samosa, 1);

        var response = await factory.ClientFor(guest).PostAsync("/api/cart/merge", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(await factory.Store.GetAsync(GuestKey(guest.SessionId!)));
    }

    [Fact]
    public async Task Without_X_Session_Id_merge_is_400_and_nothing_changes()
    {
        using var factory = new OwnedCartApiFactory();
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Samosa, 1);

        var response = await factory.ClientFor(user).PostAsync("/api/cart/merge", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, (await GetCart(factory.ClientFor(user))).Of(OwnedCartApiFactory.Samosa)!.Quantity);
    }

    [Fact]
    public async Task The_merged_cart_total_is_the_server_side_total_checkout_will_charge()
    {
        // OrderAPI and PaymentAPI both read GET /api/cart for the amount, so the merge result
        // must match what that endpoint reports afterwards.
        using var factory = new OwnedCartApiFactory();
        var guest = Caller.Guest("guest");
        await Add(factory.ClientFor(guest), OwnedCartApiFactory.Samosa, 3);
        await ApplyCoupon(factory.ClientFor(guest), OwnedCartApiFactory.ValidCoupon);
        var user = Caller.User("user");
        await Add(factory.ClientFor(user), OwnedCartApiFactory.Chai, 2);

        var merged = await Merge(factory.ClientFor(user.UserId!.Value, guest.SessionId!));
        var afterwards = await GetCart(factory.ClientFor(user));

        Assert.Equal(afterwards.Total, merged.Cart.Total);
        // 3 x 4.50 + 2 x 3.00 = 19.50, minus 1.00 coupon, + 6% tax on 18.50, + 5.00 delivery
        Assert.Equal(19.50m - 1.00m + 18.50m * 0.06m + 5.00m, afterwards.Total);
    }
}
