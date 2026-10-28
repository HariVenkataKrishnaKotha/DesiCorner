using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DesiCorner.Contracts.Payment;
using DesiCorner.Services.PaymentAPI.Models;

namespace DesiCorner.Services.PaymentAPI.Tests;

/// <summary>
/// H2: the PaymentIntent amount must come from the server-side cart total, not from the
/// client, and /verify must report what Stripe actually charged.
/// </summary>
public class PaymentAmountTests
{
    private static PaymentIntentRequestDto ClientRequest(decimal amount, string currency = "usd") => new()
    {
        OrderId = Guid.Empty,
        Amount = amount,
        Currency = currency
    };

    [Fact]
    public async Task Intent_amount_is_the_cart_total_not_the_client_amount()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused") { CartTotal = 100.00m };
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Id", "guest-session");

        var response = await client.PostAsJsonAsync("/api/payment/create-intent", ClientRequest(0.50m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = Assert.Single(factory.Stripe.Created);
        Assert.Equal(10000, created.Amount);

        var cartCall = Assert.Single(factory.Downstream.Requests);
        Assert.Equal("guest-session", cartCall.Header("X-Session-Id"));
    }

    [Fact]
    public async Task Intent_amount_is_rounded_to_whole_cents()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused") { CartTotal = 23.475m };
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Id", "guest-session");

        await client.PostAsJsonAsync("/api/payment/create-intent", ClientRequest(23.47m));

        Assert.Equal(2348, Assert.Single(factory.Stripe.Created).Amount);
    }

    [Fact]
    public async Task Authenticated_callers_cart_is_looked_up_with_their_token()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused") { CartTotal = 42.00m };
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "caller-token");

        await client.PostAsJsonAsync("/api/payment/create-intent", ClientRequest(1.00m));

        Assert.Equal(4200, Assert.Single(factory.Stripe.Created).Amount);
        Assert.Equal("Bearer caller-token", Assert.Single(factory.Downstream.Requests).Header("Authorization"));
    }

    [Fact]
    public async Task Intent_currency_is_chosen_by_the_server()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Id", "guest-session");

        await client.PostAsJsonAsync("/api/payment/create-intent", ClientRequest(100.00m, currency: "jpy"));

        Assert.Equal("usd", Assert.Single(factory.Stripe.Created).Currency);
    }

    [Fact]
    public async Task No_intent_is_created_for_an_empty_cart()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused") { CartTotal = null };
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Session-Id", "guest-session");

        var response = await client.PostAsJsonAsync("/api/payment/create-intent", ClientRequest(5.00m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Stripe.Created);
    }

    [Fact]
    public async Task Verify_reports_the_amount_and_currency_Stripe_charged()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        factory.Stripe.Add(new Stripe.PaymentIntent { Id = "pi_cheap", Amount = 50, Currency = "usd", Status = "succeeded" });
        var owner = Guid.NewGuid();
        using (var db = factory.CreateDbContext())
        {
            // The stored record says $100; only Stripe knows what was really charged.
            db.Payments.Add(new Payment { Id = Guid.NewGuid(), PaymentIntentId = "pi_cheap", UserId = owner, AmountInCents = 10000, Amount = 100m, Status = "succeeded" });
            await db.SaveChangesAsync();
        }

        var response = await factory.ClientForUser(owner).PostAsJsonAsync("/api/payment/verify", new VerifyPaymentRequest { PaymentIntentId = "pi_cheap" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var result = json.RootElement.GetProperty("result");
        Assert.True(result.TryGetProperty("amountInCents", out var amountInCents), "verify must report amountInCents");
        Assert.Equal(50, amountInCents.GetInt64());
        Assert.Equal("usd", result.GetProperty("currency").GetString());
    }
}
