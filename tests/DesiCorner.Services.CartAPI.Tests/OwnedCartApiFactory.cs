using System.Collections.Concurrent;
using System.Net.Http.Headers;
using DesiCorner.Contracts.Coupons;
using DesiCorner.Services.CartAPI.Models;
using DesiCorner.Services.CartAPI.Services;
using DesiCorner.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DesiCorner.Services.CartAPI.Tests;

/// <summary>
/// Hosts CartAPI with the real CartService and real JWT validation. Only the edges are
/// faked: carts live in memory instead of Redis, products and coupons come from fixed data.
/// </summary>
public sealed class OwnedCartApiFactory : WebApplicationFactory<Program>
{
    public const string ValidCoupon = "SAVE5";
    /// <summary>A coupon only guests may use (it fails validation once a UserId is present).</summary>
    public const string GuestOnlyCoupon = "NEWUSER";
    public static readonly Guid Samosa = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Chai = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    public InMemoryCartStore Store { get; } = new();

    /// <summary>The product catalog CartAPI sees. Tests may change prices and availability.</summary>
    public CatalogProductService Products { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.UseTestJwtBearer();

            services.RemoveAll<ICartStore>();
            services.AddSingleton<ICartStore>(Store);
            services.RemoveAll<IProductService>();
            services.AddSingleton<IProductService>(Products);
            services.RemoveAll<ICouponService>();
            services.AddSingleton<ICouponService, FixedCouponService>();

            // Nothing uses Redis any more; fail fast instead of waiting on the startup ping.
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false,connectTimeout=100,asyncTimeout=100,syncTimeout=100"));
        });
    }

    /// <summary>A signed-in user's client that also sends a guest session id, as after sign-in.</summary>
    public HttpClient ClientFor(Guid userId, string guestSessionId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId));
        client.DefaultRequestHeaders.Add("X-Session-Id", guestSessionId);
        return client;
    }

    /// <summary>A client acting as the given caller: a signed-in user or a guest session.</summary>
    public HttpClient ClientFor(Caller caller)
    {
        var client = CreateClient();
        if (caller.UserId is { } userId)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(userId));
        }
        else
        {
            client.DefaultRequestHeaders.Add("X-Session-Id", caller.SessionId);
        }
        return client;
    }
}

public sealed record Caller(string Name, Guid? UserId, string? SessionId)
{
    public static Caller User(string name) => new(name, Guid.NewGuid(), null);
    public static Caller Guest(string name) => new(name, null, "session-" + Guid.NewGuid());
    public override string ToString() => Name;
}

/// <summary>Stores JSON copies, like Redis does, so requests never share object instances.</summary>
public sealed class InMemoryCartStore : ICartStore
{
    private readonly ConcurrentDictionary<string, string> _carts = new();

    public Task<Cart?> GetAsync(string key) =>
        Task.FromResult(_carts.TryGetValue(key, out var json) ? JsonConvert.DeserializeObject<Cart>(json) : null);

    public Task SaveAsync(string key, Cart cart, TimeSpan ttl)
    {
        _carts[key] = JsonConvert.SerializeObject(cart);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        _carts.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Fixed starting catalog (Samosa $4.50, Chai $3.00) that tests can change. When
/// <see cref="Unreachable"/> is set, lookups behave like ProductAPI being down.
/// </summary>
public sealed class CatalogProductService : IProductService
{
    private readonly ConcurrentDictionary<Guid, ProductDetails> _products = new()
    {
        [OwnedCartApiFactory.Samosa] = new ProductDetails { Id = OwnedCartApiFactory.Samosa, Name = "Samosa", Price = 4.50m, IsAvailable = true },
        [OwnedCartApiFactory.Chai] = new ProductDetails { Id = OwnedCartApiFactory.Chai, Name = "Chai", Price = 3.00m, IsAvailable = true },
    };

    public bool Unreachable { get; set; }

    public void SetPrice(Guid productId, decimal price) => _products[productId].Price = price;

    public void SetAvailable(Guid productId, bool available) => _products[productId].IsAvailable = available;

    public void Remove(Guid productId) => _products.TryRemove(productId, out _);

    public Task<ProductDetails?> GetProductAsync(Guid productId, CancellationToken ct = default) =>
        // The real ProductService also returns null when ProductAPI cannot be reached
        Task.FromResult(!Unreachable && _products.TryGetValue(productId, out var p) ? Copy(p) : null);

    public Task<ProductLookup> LookupProductAsync(Guid productId, CancellationToken ct = default) =>
        Task.FromResult(
            Unreachable ? ProductLookup.Failed :
            _products.TryGetValue(productId, out var p) ? new ProductLookup(ProductLookupStatus.Found, Copy(p)) :
            ProductLookup.NotFound);

    private static ProductDetails Copy(ProductDetails p) =>
        new() { Id = p.Id, Name = p.Name, Price = p.Price, ImageUrl = p.ImageUrl, IsAvailable = p.IsAvailable };
}

public sealed class FixedCouponService : ICouponService
{
    public Task<ValidateCouponResponseDto> ValidateCouponAsync(ValidateCouponRequestDto request, CancellationToken ct = default) =>
        Task.FromResult(
            request.Code == OwnedCartApiFactory.ValidCoupon ||
            (request.Code == OwnedCartApiFactory.GuestOnlyCoupon && request.UserId == null)
                ? new ValidateCouponResponseDto { IsValid = true, DiscountAmount = 1.00m }
                : new ValidateCouponResponseDto { IsValid = false, Message = "Invalid coupon code" });

    public Task<List<CouponDto>> GetAllCouponsAsync(CouponFilterDto filter, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<CouponDto?> GetCouponByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<CouponDto?> GetCouponByCodeAsync(string code, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<CouponDto> CreateCouponAsync(CreateCouponDto dto, string createdBy, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<CouponDto?> UpdateCouponAsync(UpdateCouponDto dto, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<bool> DeleteCouponAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<bool> ToggleCouponStatusAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task IncrementUsageAsync(string code, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<CouponStatsDto> GetCouponStatsAsync(CancellationToken ct = default) => throw new NotSupportedException();
}
