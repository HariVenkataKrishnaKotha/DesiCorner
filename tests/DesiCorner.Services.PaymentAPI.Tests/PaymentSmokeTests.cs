using System.Net;
using System.Net.Http.Json;
using DesiCorner.Contracts.Payment;
using DesiCorner.Services.PaymentAPI.Models;

namespace DesiCorner.Services.PaymentAPI.Tests;

public class PaymentSmokeTests
{
    [Fact]
    public async Task Verify_reports_a_succeeded_intent()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        factory.Stripe.Add(new Stripe.PaymentIntent { Id = "pi_ok", Amount = 2500, Currency = "usd", Status = "succeeded" });
        using (var db = factory.CreateDbContext())
        {
            db.Payments.Add(new Payment { Id = Guid.NewGuid(), PaymentIntentId = "pi_ok", AmountInCents = 2500, Amount = 25m, Status = "requires_payment_method" });
            await db.SaveChangesAsync();
        }

        var response = await factory.CreateClient().PostAsJsonAsync("/api/payment/verify", new VerifyPaymentRequest { PaymentIntentId = "pi_ok" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"succeeded\"", body);
    }
}
