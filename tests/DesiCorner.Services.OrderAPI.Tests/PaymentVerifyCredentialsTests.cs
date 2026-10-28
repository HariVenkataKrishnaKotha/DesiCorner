using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DesiCorner.Contracts.Orders;
using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// Item 3: PaymentAPI's /verify now answers only the caller who created the PaymentIntent, so
/// OrderAPI must call it with the caller's own credentials.
/// </summary>
public class PaymentVerifyCredentialsTests
{
    private static RecordedRequest VerifyCall(OrderApiFactory factory) =>
        Assert.Single(factory.Downstream.Requests, r => r.Uri.AbsolutePath == "/api/payment/verify");

    [Fact]
    public async Task Signed_in_checkout_forwards_the_callers_bearer_token_to_verify()
    {
        using var factory = new OrderApiFactory();
        var token = TestJwt.CreateToken(Guid.NewGuid(), "a@example.test", "5550101");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            OrderType = "Pickup", PaymentMethod = "Stripe", PaymentIntentId = "pi_user"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer " + token, VerifyCall(factory).Header("Authorization"));
    }

    [Fact]
    public async Task Guest_checkout_sends_the_guest_session_to_verify()
    {
        using var factory = new OrderApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            Email = "guest@example.test", Phone = "5550100", SessionId = "session-s1",
            OrderType = "Pickup", PaymentMethod = "Stripe", PaymentIntentId = "pi_guest"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var verify = VerifyCall(factory);
        Assert.Equal("session-s1", verify.Header("X-Session-Id"));
        Assert.Null(verify.Header("Authorization"));
    }
}
