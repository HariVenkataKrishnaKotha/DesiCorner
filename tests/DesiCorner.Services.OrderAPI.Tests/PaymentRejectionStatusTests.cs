using System.Net;
using System.Net.Http.Json;
using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Orders;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// A payment that cannot pay for this order (wrong amount or currency, or already used by
/// another order) is a conflict with the current cart/order state: 409 with the usual
/// ResponseDto envelope. Other payment failures keep their existing status codes.
/// </summary>
public class PaymentRejectionStatusTests
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

    private static async Task<HttpResponseMessage> PlaceOrder(OrderApiFactory factory, CreateOrderDto? order = null) =>
        await factory.CreateClient().PostAsJsonAsync("/api/orders", order ?? StripeOrder());

    [Fact]
    public async Task Amount_mismatch_returns_409_with_the_rejection_message()
    {
        using var factory = new OrderApiFactory
        {
            CartTotal = 100.00m,
            VerifyResultJson = """{"isSuccess":true,"status":"succeeded","amount":0.50,"amountInCents":50,"currency":"usd"}"""
        };

        var response = await PlaceOrder(factory);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResponseDto>();
        Assert.NotNull(body);
        Assert.False(body!.IsSuccess);
        Assert.Equal("Payment amount does not match the order total.", body.Message);
    }

    [Fact]
    public async Task Currency_mismatch_returns_409()
    {
        using var factory = new OrderApiFactory
        {
            CartTotal = 100.00m,
            VerifyResultJson = """{"isSuccess":true,"status":"succeeded","amount":100.00,"amountInCents":10000,"currency":"inr"}"""
        };

        var response = await PlaceOrder(factory);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reused_payment_intent_returns_409_with_the_rejection_message()
    {
        using var factory = new OrderApiFactory();

        var first = await PlaceOrder(factory, StripeOrder("pi_once"));
        var second = await PlaceOrder(factory, StripeOrder("pi_once"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<ResponseDto>();
        Assert.NotNull(body);
        Assert.False(body!.IsSuccess);
        Assert.Equal("This payment has already been used for an order.", body.Message);
    }

    [Fact]
    public async Task Payment_not_succeeded_keeps_its_existing_500()
    {
        using var factory = new OrderApiFactory
        {
            VerifyResultJson = """{"isSuccess":true,"status":"processing","amount":100.00,"amountInCents":10000,"currency":"usd"}"""
        };

        var response = await PlaceOrder(factory);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
