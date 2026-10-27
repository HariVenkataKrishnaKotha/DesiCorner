using System.Security.Cryptography;
using System.Text;
using Stripe;

namespace DesiCorner.Services.PaymentAPI.Tests;

/// <summary>
/// Builds Stripe-shaped webhook payloads and signs them the way Stripe does
/// (t=timestamp,v1=HMAC-SHA256(secret, "{t}.{payload}")), using a secret generated per test run.
/// </summary>
public static class StripeWebhook
{
    public static string NewSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    public static string PaymentIntentEvent(string eventId, string paymentIntentId, string status) => $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "{{StripeConfiguration.ApiVersion}}",
          "created": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
          "type": "payment_intent.{{status}}",
          "livemode": false,
          "pending_webhooks": 1,
          "request": { "id": null, "idempotency_key": null },
          "data": {
            "object": {
              "id": "{{paymentIntentId}}",
              "object": "payment_intent",
              "amount": 2500,
              "currency": "usd",
              "status": "{{status}}"
            }
          }
        }
        """;

    public static string Sign(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    public static HttpRequestMessage Request(string payload, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payment/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        if (signature != null)
        {
            request.Headers.Add("Stripe-Signature", signature);
        }
        return request;
    }
}
