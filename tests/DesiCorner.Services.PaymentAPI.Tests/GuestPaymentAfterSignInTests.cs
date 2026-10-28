using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesiCorner.Contracts.Payment;

namespace DesiCorner.Services.PaymentAPI.Tests;

/// <summary>
/// A guest can sign in in another tab between create-intent and placing the order: tokens live
/// in localStorage, which every tab shares, so the order (and OrderAPI's verify call) then
/// carries a bearer token. A payment created by a guest session (no UserId) must still verify
/// for that same session, even when the caller is now signed in. A different session must not.
/// </summary>
public class GuestPaymentAfterSignInTests
{
    private static async Task<string> CreateGuestIntent(PaymentApiFactory factory, string sessionId)
    {
        var response = await factory.ClientForGuest(sessionId).PostAsJsonAsync("/api/payment/create-intent",
            new PaymentIntentRequestDto { OrderId = Guid.Empty, Amount = 100m, Currency = "usd" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("result").GetProperty("paymentIntentId").GetString()!;
    }

    private static HttpClient SignedInWithSession(PaymentApiFactory factory, Guid userId, string sessionId)
    {
        var client = factory.ClientForUser(userId);
        client.DefaultRequestHeaders.Add("X-Session-Id", sessionId);
        return client;
    }

    private static async Task<(HttpStatusCode Status, string Body)> Verify(HttpClient client, string id)
    {
        var response = await client.PostAsJsonAsync("/api/payment/verify", new VerifyPaymentRequest { PaymentIntentId = id });
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Signed_in_caller_with_the_creating_guest_session_can_verify_the_guest_payment()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        var id = await CreateGuestIntent(factory, "session-S1");

        var result = await Verify(SignedInWithSession(factory, Guid.NewGuid(), "session-S1"), id);

        Assert.Equal(HttpStatusCode.OK, result.Status);
    }

    [Fact]
    public async Task Signed_in_caller_with_another_guest_session_still_gets_the_unknown_id_404()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        var id = await CreateGuestIntent(factory, "session-S1");
        var client = SignedInWithSession(factory, Guid.NewGuid(), "session-S2");

        var foreign = await Verify(client, id);
        var unknown = await Verify(client, "pi_does_not_exist");

        Assert.Equal(HttpStatusCode.NotFound, foreign.Status);
        Assert.Equal(unknown, foreign);
    }

    [Fact]
    public async Task A_user_payment_is_not_opened_by_presenting_a_guest_session()
    {
        // Session matching only ever applies to payments that have no UserId.
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        var owner = Guid.NewGuid();
        var created = await factory.ClientForUser(owner).PostAsJsonAsync("/api/payment/create-intent",
            new PaymentIntentRequestDto { OrderId = Guid.Empty, Amount = 100m, Currency = "usd" });
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("result").GetProperty("paymentIntentId").GetString()!;

        var result = await Verify(SignedInWithSession(factory, Guid.NewGuid(), "session-S1"), id);

        Assert.Equal(HttpStatusCode.NotFound, result.Status);
    }
}
