using DesiCorner.Services.CartAPI.Models;

namespace DesiCorner.Services.CartAPI.Services;

public interface ICartService
{
    Task<Cart?> GetCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default);
    Task<Cart> AddToCartAsync(Guid? userId, string? sessionId, Guid productId, int quantity, CancellationToken ct = default);
    Task<Cart> UpdateCartItemAsync(Guid cartItemId, int quantity, CancellationToken ct = default);
    Task<bool> RemoveFromCartAsync(Guid cartItemId, CancellationToken ct = default);
    Task<bool> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default);
    Task<Cart> ApplyCouponAsync(Guid cartId, string couponCode, CancellationToken ct = default);
    Task<Cart> RemoveCouponAsync(Guid cartId, CancellationToken ct = default);
}