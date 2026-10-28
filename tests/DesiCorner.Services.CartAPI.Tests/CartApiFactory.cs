using DesiCorner.Services.CartAPI.Models;
using DesiCorner.Services.CartAPI.Services;
using DesiCorner.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace DesiCorner.Services.CartAPI.Tests;

/// <summary>
/// Hosts CartAPI with real JWT validation (against a test signing key) and a cart
/// service fake that records which identity the controller resolved.
/// </summary>
public sealed class CartApiFactory : WebApplicationFactory<Program>
{
    public RecordingCartService Carts { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.UseTestJwtBearer();

            services.RemoveAll<ICartService>();
            services.AddSingleton<ICartService>(Carts);

            // Startup pings Redis; fail fast instead of waiting for the default connect timeout.
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false,connectTimeout=100,asyncTimeout=100,syncTimeout=100"));
        });
    }
}

public sealed record CartCall(string Operation, Guid? UserId, string? SessionId);

public sealed class RecordingCartService : ICartService
{
    private readonly List<CartCall> _calls = new();

    public IReadOnlyList<CartCall> Calls { get { lock (_calls) return _calls.ToList(); } }

    private Cart Record(string op, Guid? userId, string? sessionId)
    {
        lock (_calls) _calls.Add(new CartCall(op, userId, sessionId));
        return new Cart { UserId = userId, SessionId = sessionId };
    }

    public Task<Cart?> GetCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default) =>
        Task.FromResult<Cart?>(Record("get", userId, sessionId));

    public Task<Cart> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity, CancellationToken ct = default) =>
        Task.FromResult(Record("add", userId, sessionId));

    public Task<bool> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        Record("clear", userId, sessionId);
        return Task.FromResult(true);
    }

    public Task<Cart?> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<bool> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Cart?> ApplyCouponAsync(Guid? userId, string? sessionId, Guid cartId, string couponCode, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Cart?> RemoveCouponAsync(Guid? userId, string? sessionId, Guid cartId, CancellationToken ct = default) =>
        throw new NotSupportedException();
}
