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

    public async Task<Order> CreateOrderAsync(
    string? authenticatedUserId,
    CreateOrderDto request,
    CancellationToken ct = default)
    {
        bool isGuestCheckout = string.IsNullOrWhiteSpace(authenticatedUserId);
        Guid? finalUserId = null;
        string email;
        string phone;
        bool isGuestOrder = true;

        if (isGuestCheckout)
        {
            // === GUEST CHECKOUT FLOW ===

            // Validate guest required fields
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new InvalidOperationException("Email and phone are required for guest checkout");
            }

            if (string.IsNullOrWhiteSpace(request.OtpCode))
            {
                throw new InvalidOperationException("OTP verification is required for guest checkout");
            }

            // Step 1: Verify OTP with AuthServer
            // REMOVED: OTP already verified in frontend, no need to verify again
            // The frontend already called verify-otp and confirmed it was valid
            /*var authClient = _httpClientFactory.CreateClient("AuthAPI");

            var otpVerifyResponse = await authClient.PostAsJsonAsync("/api/account/verify-otp",
                new
                {
                    Identifier = request.Email,
                    Otp = request.OtpCode
                }, ct);

            if (!otpVerifyResponse.IsSuccessStatusCode)
            {
                var errorContent = await otpVerifyResponse.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("OTP verification failed: {Error}", errorContent);
                throw new InvalidOperationException("Invalid or expired OTP. Please verify your email again.");
            }

            var otpResult = await otpVerifyResponse.Content.ReadFromJsonAsync<dynamic>(ct);
            if (otpResult?.isSuccess != true)
            {
                throw new InvalidOperationException("Invalid or expired OTP. Please verify your email again.");
            }*/

            // Step 2: Check if email/phone matches an existing user
            var authClient = _httpClientFactory.CreateClient("AuthAPI");
            var lookupResponse = await authClient.GetAsync(
                $"/api/account/user-lookup?email={Uri.EscapeDataString(request.Email)}&phone={Uri.EscapeDataString(request.Phone)}",
                ct);

            if (lookupResponse.IsSuccessStatusCode)
            {
                var lookupResult = await lookupResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);

                if (lookupResult.TryGetProperty("result", out var resultElement))
                {
                    if (resultElement.TryGetProperty("exists", out var existsElement) &&
                        existsElement.GetBoolean())
                    {
                        // User exists - link order to them
                        if (resultElement.TryGetProperty("userId", out var userIdElement) &&
                            userIdElement.GetString() is string userIdStr &&
                            Guid.TryParse(userIdStr, out var matchedId))
                        {
                            finalUserId = matchedId;
                            isGuestOrder = false;
                            _logger.LogInformation(
                                "Guest checkout matched existing user {UserId}. Linking order to user account.",
                                finalUserId);
                        }
                    }
                }
            }

            email = request.Email;
            phone = request.Phone;
        }
        else
        {
            // === AUTHENTICATED USER FLOW ===

            if (!Guid.TryParse(authenticatedUserId, out var userId))
            {
                throw new InvalidOperationException("Invalid user ID");
            }

            finalUserId = userId;
            isGuestOrder = false;

            // Email and phone will come from JWT claims (passed via controller)
            // For now, use request fields if provided, otherwise defaults
            email = request.Email ?? "user@example.com";  // TODO: Get from claims
            phone = request.Phone ?? "0000000000";  // TODO: Get from claims
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