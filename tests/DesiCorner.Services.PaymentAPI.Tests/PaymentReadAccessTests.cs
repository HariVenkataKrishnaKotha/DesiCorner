using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DesiCorner.Contracts.Payment;

namespace DesiCorner.Services.PaymentAPI.Tests;

/// <summary>
/// Item 3: POST /api/payment/verify and GET /api/payment/{paymentIntentId} reveal payment status
/// and amounts, so they answer only the caller who created the PaymentIntent: the same user
/// (bearer token), or the same guest session (X-Session-Id). Anyone else gets the same 404 as
/// for an unknown id.
/// </summary>
public class PaymentReadAccessTests
{
    public sealed record Caller(string Name, Guid? UserId, string? SessionId)
    {
        public static Caller User(string name) => new(name, Guid.NewGuid(), null);
        public static Caller Guest(string name) => new(name, null, "session-" + Guid.NewGuid());
        public static readonly Caller Anonymous = new("anonymous", null, null);
        public override string ToString() => Name;
    }

    public static TheoryData<Caller, Caller> Pairs => new()
    {
        { Caller.User("user A"), Caller.User("user B") },
        { Caller.Guest("guest S1"), Caller.Guest("guest S2") },
        { Caller.User("user A"), Caller.Guest("guest S1") },
        { Caller.Guest("guest S1"), Caller.User("user A") },
        { Caller.User("user A"), Caller.Anonymous },
        { Caller.Guest("guest S1"), Caller.Anonymous },
    };

    private static HttpClient ClientFor(PaymentApiFactory factory, Caller caller) =>
        caller.UserId is { } userId ? factory.ClientForUser(userId)
        : caller.SessionId is { } sessionId ? factory.ClientForGuest(sessionId)
        : factory.CreateClient();

    private static async Task<string> CreateIntent(PaymentApiFactory factory, Caller owner)
    {
        var response = await ClientFor(factory, owner).PostAsJsonAsync("/api/payment/create-intent",
            new PaymentIntentRequestDto { OrderId = Guid.Empty, Amount = 100m, Currency = "usd" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("result").GetProperty("paymentIntentId").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, string Body)> Verify(PaymentApiFactory factory, Caller caller, string id)
    {
        var response = await ClientFor(factory, caller).PostAsJsonAsync("/api/payment/verify", new VerifyPaymentRequest { PaymentIntentId = id });
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<(HttpStatusCode Status, string Body)> Get(PaymentApiFactory factory, Caller caller, string id)
    {
        var response = await ClientFor(factory, caller).GetAsync($"/api/payment/{id}");
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Verify_answers_only_the_caller_who_created_the_intent(Caller owner, Caller other)
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        var id = await CreateIntent(factory, owner);

        Assert.Equal(HttpStatusCode.OK, (await Verify(factory, owner, id)).Status);

        var foreign = await Verify(factory, other, id);
        var unknown = await Verify(factory, other, "pi_does_not_exist");
        Assert.Equal(HttpStatusCode.NotFound, foreign.Status);
        Assert.Equal(unknown, foreign);
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Get_answers_only_the_caller_who_created_the_intent(Caller owner, Caller other)
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        var id = await CreateIntent(factory, owner);

        var own = await Get(factory, owner, id);
        Assert.Equal(HttpStatusCode.OK, own.Status);
        Assert.Contains(id, own.Body);

        var foreign = await Get(factory, other, id);
        var unknown = await Get(factory, other, "pi_does_not_exist");
        Assert.Equal(HttpStatusCode.NotFound, foreign.Status);
        Assert.Equal(unknown, foreign);
    }

    [Fact]
    public async Task Verify_by_a_non_owner_does_not_touch_Stripe_or_the_stored_payment()
    {
        using var factory = new PaymentApiFactory(webhookSecret: "unused");
        var owner = Caller.User("user A");
        var id = await CreateIntent(factory, owner);
        factory.Stripe.Add(new Stripe.PaymentIntent { Id = id, Amount = 10000, Currency = "usd", Status = "succeeded" });

        await Verify(factory, Caller.User("user B"), id);

        using var db = factory.CreateDbContext();
        Assert.Equal("requires_payment_method", db.Payments.Single(p => p.PaymentIntentId == id).Status);
    }
}
