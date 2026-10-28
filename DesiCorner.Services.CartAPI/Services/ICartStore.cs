using DesiCorner.Services.CartAPI.Models;

namespace DesiCorner.Services.CartAPI.Services;

/// <summary>
/// Persistence for carts, keyed by owner ("cart:user:{id}" or "cart:session:{id}").
/// </summary>
public interface ICartStore
{
    Task<Cart?> GetAsync(string key);
    Task SaveAsync(string key, Cart cart, TimeSpan ttl);
    Task DeleteAsync(string key);
}
