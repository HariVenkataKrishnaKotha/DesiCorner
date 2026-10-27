using System.Net;
using System.Net.Http.Json;
using DesiCorner.Contracts.Orders;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// H2: a Stripe order may only be created when the PaymentIntent actually charged the
/// server-computed cart total, in the right currency, and has not paid for another order.
/// </summary>
public class PaymentAmountTests
{
    private static CreateOrderDto StripeOrder(string paymentIntentId = "pi_test") => new()
    {
        Email = "guest@example.test",
        Phone = "5550100",
        SessionId = "guest-session",
        OrderType = "Pickup",
        PaymentMethod = "Stripe",
        PaymentIntentId = paymentIntentId
    };

    private static string Verified(long amountInCents, string currency) =>
        $$"""{"isSuccess":true,"status":"succeeded","amount":{{amountInCents / 100m}},"amountInCents":{{amountInCents}},"currency":"{{currency}}"}""";

    private static async Task<HttpResponseMessage> PlaceOrder(OrderApiFactory factory, CreateOrderDto? order = null) =>
        await factory.CreateClient().PostAsJsonAsync("/api/orders", order ?? StripeOrder());

    [Fact]
    public async Task Order_is_created_when_the_intent_paid_the_cart_total()
    {
        using var factory = new OrderApiFactory { CartTotal = 19.99m, VerifyResultJson = Verified(1999, "usd") };

        var response = await PlaceOrder(factory);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var db = factory.CreateDbContext();
        var order = Assert.Single(db.Orders);
        Assert.Equal(19.99m, order.Total);
        Assert.Equal("pi_test", order.PaymentIntentId);
    }

    [Fact]
    public async Task Order_is_rejected_when_the_intent_paid_less_than_the_cart_total()
    {
        using var factory = new OrderApiFactory { CartTotal = 100.00m, VerifyResultJson = Verified(50, "usd") };

        var response = await PlaceOrder(factory);

        Assert.False(response.IsSuccessStatusCode);
        using var db = factory.CreateDbContext();
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Order_is_rejected_when_the_intent_paid_more_than_the_cart_total()
    {
        // The cart changed after payment; the customer must not get a different order than they paid for.
        using var factory = new OrderApiFactory { CartTotal = 10.00m, VerifyResultJson = Verified(1500, "usd") };

        var response = await PlaceOrder(factory);

        Assert.False(response.IsSuccessStatusCode);
        using var db = factory.CreateDbContext();
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Order_is_rejected_when_the_currency_differs()
    {
        using var factory = new OrderApiFactory { CartTotal = 100.00m, VerifyResultJson = Verified(10000, "inr") };

        var response = await PlaceOrder(factory);

        Assert.False(response.IsSuccessStatusCode);
        using var db = factory.CreateDbContext();
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Order_is_rejected_when_the_paid_amount_is_not_reported()
    {
        // Fail closed if PaymentAPI does not say what the intent actually charged.
        using var factory = new OrderApiFactory
        {
            CartTotal = 100.00m,
            VerifyResultJson = """{"isSuccess":true,"status":"succeeded","amount":100.00}"""
        };

        var response = await PlaceOrder(factory);

        Assert.False(response.IsSuccessStatusCode);
        using var db = factory.CreateDbContext();
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task A_payment_intent_cannot_pay_for_two_orders()
    {
        using var factory = new OrderApiFactory { CartTotal = 100.00m, VerifyResultJson = Verified(10000, "usd") };

        var first = await PlaceOrder(factory, StripeOrder("pi_once"));
        var second = await PlaceOrder(factory, StripeOrder("pi_once"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.False(second.IsSuccessStatusCode);
        using var db = factory.CreateDbContext();
        Assert.Single(db.Orders);
    }
}
