using DesiCorner.Contracts.Orders;
using DesiCorner.Services.OrderAPI.Data;
using DesiCorner.Services.OrderAPI.Models;
using Microsoft.EntityFrameworkCore;
using DesiCorner.Contracts.Common;

namespace DesiCorner.Services.OrderAPI.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrderService(
        OrderDbContext context,
        ILogger<OrderService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Order> CreateOrderAsync(string? authenticatedUserId, CreateOrderDto request, string? email, string? phone, CancellationToken ct)
    {
        // Determine if this is a guest or authenticated user checkout
        bool isGuestCheckout = string.IsNullOrWhiteSpace(authenticatedUserId);
        Guid? finalUserId = null;
        bool isGuestOrder = true;

        if (isGuestCheckout)
        {
            // === GUEST CHECKOUT FLOW ===
            // Validate guest fields
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new ArgumentException("Email and Phone are required for guest checkout");
            }

            email = request.Email;
            phone = request.Phone;

            // Check if this email/phone matches an existing registered user
            try
            {
                var authClient = _httpClientFactory.CreateClient("AuthServer");
                var lookupResponse = await authClient.GetAsync(
                    $"/api/account/user-lookup?email={Uri.EscapeDataString(request.Email)}&phone={Uri.EscapeDataString(request.Phone)}",
                    ct);

                if (lookupResponse.IsSuccessStatusCode)
                {
                    var lookupData = await lookupResponse.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken: ct);
                    if (lookupData?.IsSuccess == true && lookupData.Result != null)
                    {
                        // Extract UserId from anonymous object
                        var resultJson = System.Text.Json.JsonSerializer.Serialize(lookupData.Result);
                        var resultDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson);

                        if (resultDict != null && resultDict.ContainsKey("UserId"))
                        {
                            var userIdValue = resultDict["UserId"]?.ToString();
                            if (!string.IsNullOrEmpty(userIdValue) && Guid.TryParse(userIdValue, out var parsedUserId))
                            {
                                // Link this order to the existing user
                                finalUserId = parsedUserId;
                                isGuestOrder = false;
                                _logger.LogInformation("Linking guest order to existing user: {UserId}", finalUserId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not verify user lookup, treating as guest order");
            }
        }
        else
        {
            // === AUTHENTICATED USER FLOW ===
            if (!string.IsNullOrWhiteSpace(authenticatedUserId))
            {
                finalUserId = Guid.Parse(authenticatedUserId);
                isGuestOrder = false;
            }

            // Use email and phone passed from controller (from forwarded headers)
            // These should now have real values from JWT claims
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("No email forwarded for authenticated user {UserId}", finalUserId);
                email = "user@example.com";
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                _logger.LogWarning("No phone forwarded for authenticated user {UserId}", finalUserId);
                phone = "0000000000";
            }
        }

        // === CREATE ORDER ===

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            UserId = finalUserId,  // null for true guests, set for matched/authenticated users
            IsGuestOrder = isGuestOrder,
            UserEmail = email,
            UserPhone = phone,

            // Delivery information from request
            DeliveryAddress = request.DeliveryAddress,
            DeliveryCity = request.DeliveryCity,
            DeliveryState = request.DeliveryState,
            DeliveryZipCode = request.DeliveryZipCode,
            SpecialInstructions = request.DeliveryInstructions,

            // Pricing (TODO: Calculate from cart)
            SubTotal = 0,
            TaxAmount = 0,
            DeliveryFee = 0,
            DiscountAmount = 0,
            Total = 0,

            // Status
            Status = "Pending",
            PaymentStatus = "Pending",
            PaymentMethod = request.PaymentMethod,
            PaymentIntentId = request.PaymentIntentId,

            // Timestamps
            OrderDate = DateTime.UtcNow,
            EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(45),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {OrderNumber} created successfully. IsGuest: {IsGuest}, UserId: {UserId}, Email: {Email}",
            order.OrderNumber, isGuestOrder, finalUserId, email);

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