using DesiCorner.Contracts.Orders;
using DesiCorner.Services.OrderAPI.Models;

namespace DesiCorner.Services.OrderAPI.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(Guid userId, string userEmail, string userPhone, CreateOrderDto request, CancellationToken ct = default);
    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Order?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<List<Order>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Order> UpdateOrderStatusAsync(Guid orderId, string status, string? notes = null, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(Guid orderId, Guid userId, CancellationToken ct = default);
    Task<int> GetUserOrderCountAsync(Guid userId, CancellationToken ct = default);
}