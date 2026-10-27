using DesiCorner.Contracts.Cart;

namespace DesiCorner.Services.PaymentAPI.Services;

public interface ICartClient
{
    /// <summary>
    /// Fetches the current caller's cart from CartAPI, or null if it cannot be read.
    /// </summary>
    Task<CartDto?> GetCallerCartAsync(CancellationToken ct = default);
}
