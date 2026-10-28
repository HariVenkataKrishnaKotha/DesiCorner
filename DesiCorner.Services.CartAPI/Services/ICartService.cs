using DesiCorner.Services.CartAPI.Models;

namespace DesiCorner.Services.CartAPI.Services;

public interface ICartService
{
    Task<Cart?> GetCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default);
    Task<Cart> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity, CancellationToken ct = default);
    Task<bool> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default);

    // The operations below act only inside the caller's own cart (by userId, else sessionId).
    // They return null/false when the id is not in that cart, whether it doesn't exist or
    // belongs to someone else, so callers cannot tell the two apart.
    Task<Cart?> UpdateCartItemAsync(Guid? userId, string? sessionId, Guid cartItemId, int quantity, CancellationToken ct = default);
    Task<bool> RemoveFromCartAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken ct = default);
    Task<Cart?> ApplyCouponAsync(Guid? userId, string? sessionId, Guid cartId, string couponCode, CancellationToken ct = default);
    Task<Cart?> RemoveCouponAsync(Guid? userId, string? sessionId, Guid cartId, CancellationToken ct = default);
}