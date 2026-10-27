using DesiCorner.Services.PaymentAPI.Data;
using DesiCorner.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stripe;

namespace DesiCorner.Services.PaymentAPI.Tests;

/// <summary>
/// Hosts PaymentAPI with an in-memory database and a fake Stripe PaymentIntentService.
/// Nothing here talks to Stripe.
/// </summary>
public sealed class PaymentApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly string _environment;
    private readonly string? _webhookSecret;

    public PaymentApiFactory(string? webhookSecret, string environment = "Testing")
    {
        _webhookSecret = webhookSecret;
        _environment = environment;
    }

    public FakePaymentIntentService Stripe { get; } = new();

    public RecordingHandler Downstream { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Tests must never pick up a real secret from the environment.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:WebhookSecret"] = _webhookSecret ?? ""
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PaymentDbContext>>();
            services.AddDbContext<PaymentDbContext>(o => o.UseInMemoryDatabase(_dbName));

            services.RemoveAll<PaymentIntentService>();
            services.AddSingleton<PaymentIntentService>(Stripe);

            services.AddHttpClient("CartAPI").ConfigurePrimaryHttpMessageHandler(() => Downstream);
        });
    }

    public PaymentDbContext CreateDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<PaymentDbContext>();
}

public sealed class FakePaymentIntentService : PaymentIntentService
{
    private readonly Dictionary<string, PaymentIntent> _intents = new();

    public List<PaymentIntentCreateOptions> Created { get; } = new();

    public void Add(PaymentIntent intent)
    {
        lock (_intents) _intents[intent.Id] = intent;
    }

    public override Task<PaymentIntent> CreateAsync(
        PaymentIntentCreateOptions options,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        var intent = new PaymentIntent
        {
            Id = "pi_fake_" + Guid.NewGuid().ToString("N"),
            Amount = options.Amount ?? 0,
            Currency = options.Currency,
            Status = "requires_payment_method",
            ClientSecret = "fake_client_secret",
            Metadata = options.Metadata
        };
        lock (_intents)
        {
            Created.Add(options);
            _intents[intent.Id] = intent;
        }
        return Task.FromResult(intent);
    }

    public override Task<PaymentIntent> GetAsync(
        string id,
        PaymentIntentGetOptions? options = null,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        lock (_intents)
        {
            return _intents.TryGetValue(id, out var intent)
                ? Task.FromResult(intent)
                : throw new StripeException($"No such payment_intent: '{id}'");
        }
    }
}
