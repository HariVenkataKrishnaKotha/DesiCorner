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
/// and a recording handler standing in for CartAPI (via the gateway) and PaymentAPI.
/// </summary>
public sealed class OrderApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

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

    public Func<RecordedRequest, HttpResponseMessage> Respond { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.UseTestJwtBearer();

            services.RemoveAll<DbContextOptions<OrderDbContext>>();
            services.AddDbContext<OrderDbContext>(o => o.UseInMemoryDatabase(_dbName));

            foreach (var name in new[] { "CartAPI", "PaymentAPI", "AuthAPI", "AuthServer" })
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

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    public OrderDbContext CreateDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<OrderDbContext>();
}
