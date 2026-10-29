using System.Net;
using System.Text.Json;
using DesiCorner.Contracts.Cart;
using DesiCorner.Contracts.Common;
using DesiCorner.Services.OrderAPI.Data;
using DesiCorner.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// Hosts OrderAPI with an in-memory database, real JWT validation against a test key,
/// and a recording handler standing in for CartAPI (via the gateway), PaymentAPI and
/// AuthServer (token endpoint and user lookup).
/// </summary>
public sealed class OrderApiFactory : WebApplicationFactory<Program>
{
    public const string ServiceSecretKey = "ServiceAuth:ClientSecret";
    public const string TestServiceSecret = "order-service-test-secret";
    public const string IssuedTokenPrefix = "service-token-";

    private readonly string _dbName = Guid.NewGuid().ToString();
    private int _tokensIssued;

    public OrderApiFactory()
    {
        Respond = DefaultResponse;
        Downstream = new RecordingHandler(r => Respond(r));
    }

    public RecordingHandler Downstream { get; }

    /// <summary>Cart total CartAPI reports. Defaults to $100.00.</summary>
    public decimal CartTotal { get; set; } = 100.00m;

    /// <summary>JSON "result" PaymentAPI returns from /api/payment/verify.</summary>
    public string VerifyResultJson { get; set; } =
        """{"isSuccess":true,"status":"succeeded","amount":100.00,"amountInCents":10000,"currency":"usd"}""";

    /// <summary>When set, the AuthServer user lookup reports this registered user for any guest.</summary>
    public Guid? RegisteredUserId { get; set; }

    public Func<RecordedRequest, HttpResponseMessage> Respond { get; set; }

    public string EnvironmentName { get; init; } = "Testing";

    /// <summary>OrderAPI's client-credentials secret. Null leaves it unconfigured.</summary>
    public string? ServiceSecret { get; init; } = TestServiceSecret;

    /// <summary>expires_in the stub token endpoint reports.</summary>
    public int TokenLifetimeSeconds { get; set; } = 3600;

    /// <summary>Status the stub token endpoint answers with; anything but 200 is an error.</summary>
    public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;

    public ManualClock Clock { get; } = new();

    public IReadOnlyList<RecordedRequest> TokenRequests =>
        Downstream.Requests.Where(r => r.Uri.AbsolutePath == "/connect/token").ToList();

    public IReadOnlyList<RecordedRequest> LookupRequests =>
        Downstream.Requests.Where(r => r.Uri.AbsolutePath == "/api/account/user-lookup").ToList();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        if (ServiceSecret != null)
        {
            builder.UseSetting(ServiceSecretKey, ServiceSecret);
        }

        builder.ConfigureTestServices(services =>
        {
            services.UseTestJwtBearer();

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.RemoveAll<DbContextOptions<OrderDbContext>>();
            services.AddDbContext<OrderDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Only the clients Program registers; their base addresses come from Program too.
            foreach (var name in new[] { "CartAPI", "PaymentAPI", "AuthAPI", "AuthServerToken" })
            {
                services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => Downstream);
            }
        });
    }

    public HttpResponseMessage DefaultResponse(RecordedRequest request)
    {
        var path = request.Uri.AbsolutePath;

        if (request.Method == HttpMethod.Get && path == "/api/cart")
        {
            return RecordingHandler.Json(JsonSerializer.Serialize(new ResponseDto
            {
                Result = new CartDto
                {
                    Id = Guid.NewGuid(),
                    Items = new() { new CartItemDto { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Samosa", Price = CartTotal, Quantity = 1 } },
                    SubTotal = CartTotal,
                    Total = CartTotal
                }
            }));
        }

        if (request.Method == HttpMethod.Delete && path == "/api/cart/clear")
        {
            return RecordingHandler.Json("""{"isSuccess":true,"message":"Cart cleared successfully"}""");
        }

        if (request.Method == HttpMethod.Post && path == "/api/payment/verify")
        {
            return RecordingHandler.Json("""{"isSuccess":true,"message":"","result":""" + VerifyResultJson + "}");
        }

        if (request.Method == HttpMethod.Post && path == "/connect/token")
        {
            if (TokenStatus != HttpStatusCode.OK)
            {
                return RecordingHandler.Json("""{"error":"invalid_client"}""", TokenStatus);
            }

            var token = IssuedTokenPrefix + Interlocked.Increment(ref _tokensIssued);
            return RecordingHandler.Json(
                $$"""{"access_token":"{{token}}","token_type":"Bearer","expires_in":{{TokenLifetimeSeconds}}}""");
        }

        // Like AuthServer, the lookup answers only callers holding a service token
        if (request.Method == HttpMethod.Get && path == "/api/account/user-lookup")
        {
            if (request.Header("Authorization")?.StartsWith("Bearer " + IssuedTokenPrefix) != true)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (RegisteredUserId is { } registered)
            {
                return RecordingHandler.Json(JsonSerializer.Serialize(new ResponseDto { Result = new { UserId = registered } }));
            }
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    public OrderDbContext CreateDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<OrderDbContext>();
}
