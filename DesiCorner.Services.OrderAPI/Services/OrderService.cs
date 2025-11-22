using DesiCorner.Contracts.Orders;
using DesiCorner.Services.OrderAPI.Data;
using DesiCorner.Services.OrderAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.OrderAPI.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly HttpClient _httpClient;

    public OrderService(
        OrderDbContext context,
        ILogger<OrderService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("CartAPI");
    }

    public async Task<Order> CreateOrderAsync(
        Guid userId,
        string userEmail,
        string userPhone,
        CreateOrderDto request,
        CancellationToken ct = default)
    {
        // For now, we'll create a placeholder order
        // In production, you'd fetch cart data from CartAPI

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            UserEmail = userEmail,
            UserPhone = userPhone,
            DeliveryAddress = "123 Main St", // Will come from user's address
            DeliveryCity = "City",
            DeliveryState = "State",
            DeliveryZipCode = "12345",
            SubTotal = 0,
            TaxAmount = 0,
            DeliveryFee = 0,
            DiscountAmount = 0,
            Total = 0,
            Status = "Pending",
            PaymentStatus = "Pending",
            PaymentMethod = request.PaymentMethod ?? "Stripe",
            SpecialInstructions = request.SpecialInstructions,
            OrderDate = DateTime.UtcNow,
            EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(45),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderNumber} created for user {UserId}", order.OrderNumber, userId);

        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }

    public async Task<List<Order>> GetUserOrdersAsync(
        Guid userId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<Order> UpdateOrderStatusAsync(
        Guid orderId,
        string status,
        string? notes = null,
        CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        var oldStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == "Delivered")
        {
            order.DeliveredAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {OrderNumber} status updated from {OldStatus} to {NewStatus}",
            order.OrderNumber, oldStatus, status);

        return order;
    }

    public async Task<bool> CancelOrderAsync(Guid orderId, Guid userId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

        if (order == null)
        {
            return false;
        }

        // Only allow cancellation of pending or confirmed orders
        if (order.Status != "Pending" && order.Status != "Confirmed")
        {
            throw new InvalidOperationException($"Cannot cancel order with status: {order.Status}");
        }

        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderNumber} cancelled by user {UserId}", order.OrderNumber, userId);

        return true;
    }

    public async Task<int> GetUserOrderCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Orders
            .CountAsync(o => o.UserId == userId, ct);
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
        var random = new Random().Next(1000, 9999);
        return $"DC-{timestamp}-{random}";
    }
}