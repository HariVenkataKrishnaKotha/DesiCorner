using System.Net;
using DesiCorner.Services.PaymentAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.PaymentAPI.Tests;

/// <summary>
/// H3: the webhook must only act on events whose Stripe signature verifies, must refuse
/// to run without a signing secret, and must process each event id at most once.
/// </summary>
public class WebhookTests
{
    private const string IntentId = "pi_hook";

    private static async Task SeedPayment(PaymentApiFactory factory, string status = "requires_payment_method")
    {
        using var db = factory.CreateDbContext();
        db.Payments.Add(new Payment { Id = Guid.NewGuid(), PaymentIntentId = IntentId, AmountInCents = 2500, Amount = 25m, Status = status });
        await db.SaveChangesAsync();
    }

    private static async Task<string> PaymentStatus(PaymentApiFactory factory)
    {
        using var db = factory.CreateDbContext();
        return (await db.Payments.AsNoTracking().SingleAsync(p => p.PaymentIntentId == IntentId)).Status;
    }

    [Fact]
    public async Task Unsigned_event_is_not_processed_when_no_secret_is_configured()
    {
        using var factory = new PaymentApiFactory(webhookSecret: null, environment: "Development");
        await SeedPayment(factory);
        var payload = StripeWebhook.PaymentIntentEvent("evt_unsigned", IntentId, "succeeded");

        var response = await factory.CreateClient().SendAsync(StripeWebhook.Request(payload, signature: null));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("requires_payment_method", await PaymentStatus(factory));
    }

    [Fact]
    public async Task Event_is_not_processed_when_no_secret_is_configured_even_if_it_carries_a_signature()
    {
        using var factory = new PaymentApiFactory(webhookSecret: null, environment: "Development");
        await SeedPayment(factory);
        var payload = StripeWebhook.PaymentIntentEvent("evt_unverifiable", IntentId, "succeeded");

        var response = await factory.CreateClient().SendAsync(
            StripeWebhook.Request(payload, StripeWebhook.Sign(payload, StripeWebhook.NewSecret())));

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal("requires_payment_method", await PaymentStatus(factory));
    }

    [Fact]
    public async Task Event_with_a_bad_signature_is_rejected()
    {
        using var factory = new PaymentApiFactory(webhookSecret: StripeWebhook.NewSecret());
        await SeedPayment(factory);
        var payload = StripeWebhook.PaymentIntentEvent("evt_forged", IntentId, "succeeded");

        var response = await factory.CreateClient().SendAsync(
            StripeWebhook.Request(payload, StripeWebhook.Sign(payload, StripeWebhook.NewSecret())));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("requires_payment_method", await PaymentStatus(factory));
    }

    [Fact]
    public async Task Correctly_signed_event_updates_the_payment()
    {
        var secret = StripeWebhook.NewSecret();
        using var factory = new PaymentApiFactory(secret);
        await SeedPayment(factory);
        var payload = StripeWebhook.PaymentIntentEvent("evt_good", IntentId, "succeeded");

        var response = await factory.CreateClient().SendAsync(
            StripeWebhook.Request(payload, StripeWebhook.Sign(payload, secret)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("succeeded", await PaymentStatus(factory));
    }

    [Fact]
    public async Task Redelivered_event_is_processed_only_once()
    {
        var secret = StripeWebhook.NewSecret();
        using var factory = new PaymentApiFactory(secret);
        await SeedPayment(factory);
        var client = factory.CreateClient();
        var payload = StripeWebhook.PaymentIntentEvent("evt_once", IntentId, "succeeded");

        var first = await client.SendAsync(StripeWebhook.Request(payload, StripeWebhook.Sign(payload, secret)));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Something else moves the payment on; a replay of the old event must not undo it.
        using (var db = factory.CreateDbContext())
        {
            var payment = await db.Payments.SingleAsync(p => p.PaymentIntentId == IntentId);
            payment.Status = "refunded";
            await db.SaveChangesAsync();
        }

        var replay = await client.SendAsync(StripeWebhook.Request(payload, StripeWebhook.Sign(payload, secret)));

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode); // acknowledged, so Stripe stops retrying
        Assert.Equal("refunded", await PaymentStatus(factory));
    }

    [Fact]
    public void Startup_fails_outside_Development_without_a_webhook_secret()
    {
        using var factory = new PaymentApiFactory(webhookSecret: null, environment: "Production");

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Stripe:WebhookSecret", error.ToString());
    }

    [Fact]
    public void Startup_succeeds_outside_Development_with_a_webhook_secret()
    {
        using var factory = new PaymentApiFactory(StripeWebhook.NewSecret(), environment: "Production");

        factory.CreateClient();
    }
}
