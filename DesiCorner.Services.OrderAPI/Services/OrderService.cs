using DesCorner.Contracts.Orders;
using DesiCorner.Contracts.Cart;
using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Orders;
using DesiCorner.Contracts.Payment;
using DesiCorner.Services.OrderAPI.Data;
using DesiCorner.Services.OrderAPI.Models;
using Microsoft.EntityFrameworkCore;

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

        // === VERIFY PAYMENT (only for Stripe payments) ===
        if (request.PaymentMethod == "Stripe")
        {
            if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                throw new ArgumentException("PaymentIntentId is required for Stripe payments");
            }

            _logger.LogInformation("Verifying payment: {PaymentIntentId}", request.PaymentIntentId);

            var paymentClient = _httpClientFactory.CreateClient("PaymentAPI");
            var verifyRequest = new VerifyPaymentRequest
            {
                PaymentIntentId = request.PaymentIntentId
            };

            var verifyResponse = await paymentClient.PostAsJsonAsync("/api/payment/verify", verifyRequest, ct);

            if (!verifyResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Payment verification failed with status: {StatusCode}", verifyResponse.StatusCode);
                throw new InvalidOperationException("Payment verification failed. Please try again.");
            }

            var verifyData = await verifyResponse.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken: ct);
            if (verifyData?.IsSuccess != true || verifyData.Result == null)
            {
                throw new InvalidOperationException("Payment verification failed. Invalid response from payment service.");
            }

            // Deserialize verification result
            var verifyJson = System.Text.Json.JsonSerializer.Serialize(verifyData.Result);
            var verifyResult = System.Text.Json.JsonSerializer.Deserialize<VerifyPaymentResponse>(
                verifyJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (verifyResult == null || !verifyResult.IsSuccess)
            {
                var errorMsg = verifyResult?.ErrorMessage ?? "Payment not confirmed";
                _logger.LogWarning("Payment verification failed: {Error}", errorMsg);
                throw new InvalidOperationException($"Payment failed: {errorMsg}");
            }

            if (verifyResult.Status != "succeeded")
            {
                _logger.LogWarning("Payment not succeeded. Status: {Status}", verifyResult.Status);
                throw new InvalidOperationException($"Payment not completed. Status: {verifyResult.Status}");
            }

            _logger.LogInformation("Payment verified successfully: {PaymentIntentId}, Amount: {Amount}",
                request.PaymentIntentId, verifyResult.Amount);
        }
        else if (request.PaymentMethod == "PayAtPickup")
        {
            // Validate it's a pickup order
            if (request.OrderType != "Pickup")
            {
                throw new ArgumentException("Pay at Pickup is only available for pickup orders");
            }
            _logger.LogInformation("PayAtPickup order - skipping payment verification");
        }

        // Validate delivery address for delivery orders
        if (request.OrderType == "Delivery")
        {
            if (string.IsNullOrWhiteSpace(request.DeliveryAddress) ||
                string.IsNullOrWhiteSpace(request.DeliveryCity) ||
                string.IsNullOrWhiteSpace(request.DeliveryState) ||
                string.IsNullOrWhiteSpace(request.DeliveryZipCode))
            {
                throw new ArgumentException("Delivery address is required for delivery orders");
            }
        }

        // === FETCH CART FROM CARTAPI ===
        _logger.LogInformation("Fetching cart for user {UserId} or session {SessionId}", finalUserId, request.SessionId);

        var cartClient = _httpClientFactory.CreateClient("CartAPI");
        HttpResponseMessage cartResponse;

        // Build cart request URL based on user type
        if (finalUserId.HasValue)
        {
            // Authenticated user - fetch by userId via X-Forwarded-UserId header
            var cartRequest = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
            cartRequest.Headers.Add("X-Forwarded-UserId", finalUserId.Value.ToString());
            cartResponse = await cartClient.SendAsync(cartRequest, ct);

            _logger.LogInformation("Cart API response status: {StatusCode}", cartResponse.StatusCode);

            var responseBody = await cartResponse.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Cart API raw response: {Response}", responseBody);
        }
        else
        {
            // Guest user - fetch by sessionId via X-Session-Id header
            var cartRequest = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
            cartRequest.Headers.Add("X-Session-Id", request.SessionId);
            cartResponse = await cartClient.SendAsync(cartRequest, ct);

            _logger.LogInformation("Cart API response status: {StatusCode}", cartResponse.StatusCode);

            var responseBody = await cartResponse.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Cart API raw response: {Response}", responseBody);
        }

        if (!cartResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch cart from CartAPI: {StatusCode}", cartResponse.StatusCode);
            throw new InvalidOperationException("Unable to fetch cart. Please try again.");
        }

        var cartData = await cartResponse.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken: ct);
        if (cartData?.IsSuccess != true || cartData.Result == null)
        {
            throw new InvalidOperationException("Cart is empty or unavailable. Please add items to cart before checkout.");
        }

        // Deserialize cart from anonymous Result object
        var cartJson = System.Text.Json.JsonSerializer.Serialize(cartData.Result);
        _logger.LogInformation("Serialized cart JSON: {CartJson}", cartJson);

        var deserializeOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var cart = System.Text.Json.JsonSerializer.Deserialize<CartDto>(cartJson, deserializeOptions);

        _logger.LogInformation("Deserialized cart: IsNull={IsNull}, ItemsCount={Count}",
            cart == null, cart?.Items?.Count ?? -1);

        if (cart?.Items != null)
        {
            _logger.LogInformation("Cart items details: {@Items}", cart.Items);
        }

        if (cart == null || cart.Items == null || cart.Items.Count == 0)
        {
            _logger.LogError("Cart deserialization failed or items are empty");
            throw new InvalidOperationException("Cart is empty. Please add items before placing an order.");
        }

        _logger.LogInformation("Cart fetched successfully with {ItemCount} items, Total: {Total}",
            cart.Items.Count, cart.Total);

        // === CREATE ORDER FROM CART ===

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            UserId = finalUserId,
            IsGuestOrder = isGuestOrder,
            UserEmail = email,
            UserPhone = phone,

            // Delivery information from request
            DeliveryAddress = request.DeliveryAddress,
            DeliveryCity = request.DeliveryCity,
            DeliveryState = request.DeliveryState,
            DeliveryZipCode = request.DeliveryZipCode,
            SpecialInstructions = request.DeliveryInstructions,

            // Pricing from cart
            SubTotal = cart.SubTotal,
            TaxAmount = cart.TaxAmount,
            DeliveryFee = cart.DeliveryFee,
            DiscountAmount = cart.DiscountAmount,
            Total = cart.Total,
            CouponCode = cart.CouponCode,

            // Status
            Status = request.PaymentMethod == "Stripe" ? "Confirmed" : "Pending",
            PaymentStatus = request.PaymentMethod == "Stripe" ? "Paid" : "Pending",
            PaymentMethod = request.PaymentMethod,
            PaymentIntentId = request.PaymentIntentId,

            // Timestamps
            OrderDate = DateTime.UtcNow,
            EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(45),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderType = request.OrderType,
            ScheduledPickupTime = request.OrderType == "Pickup" ? DateTime.UtcNow.AddMinutes(20) : null,
        };

        // Convert CartItems to OrderItems
        foreach (var cartItem in cart.Items)
        {
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                ProductImage = cartItem.ProductImage,
                Price = cartItem.Price,
                Quantity = cartItem.Quantity
            };
            order.Items.Add(orderItem);
        }

        _logger.LogInformation("Created order with {ItemCount} items from cart", order.Items.Count);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        // === CLEAR CART AFTER SUCCESSFUL ORDER ===
        try
        {
            _logger.LogInformation("Clearing cart for user {UserId} or session {SessionId}", finalUserId, request.SessionId);

            HttpResponseMessage clearResponse;

            if (finalUserId.HasValue)
            {
                // Authenticated user - clear by userId
                var clearRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/cart/clear");
                clearRequest.Headers.Add("X-Forwarded-UserId", finalUserId.Value.ToString());
                clearResponse = await cartClient.SendAsync(clearRequest, ct);
            }
            else
            {
                // Guest user - clear by sessionId
                var clearRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/cart/clear");
                clearRequest.Headers.Add("X-Session-Id", request.SessionId);
                clearResponse = await cartClient.SendAsync(clearRequest, ct);
            }

            if (clearResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Cart cleared successfully after order creation");
            }
            else
            {
                _logger.LogWarning("Failed to clear cart after order creation: {StatusCode}", clearResponse.StatusCode);
                // Don't throw - order was already created successfully
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing cart after order creation - non-critical");
            // Don't throw - order was already created successfully
        }

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

    public async Task<(List<AdminOrderListDto> Orders, int TotalCount)> GetAllOrdersAsync(
    AdminOrderFilterDto filter,
    CancellationToken ct = default)
    {
        var query = _context.Orders.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(o => o.Status == filter.Status);
        }

        if (!string.IsNullOrEmpty(filter.PaymentStatus))
        {
            query = query.Where(o => o.PaymentStatus == filter.PaymentStatus);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(term) ||
                o.UserEmail.ToLower().Contains(term));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= filter.ToDate.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply sorting
        query = filter.SortBy.ToLower() switch
        {
            "ordernumber" => filter.SortDescending
                ? query.OrderByDescending(o => o.OrderNumber)
                : query.OrderBy(o => o.OrderNumber),
            "total" => filter.SortDescending
                ? query.OrderByDescending(o => o.Total)
                : query.OrderBy(o => o.Total),
            "status" => filter.SortDescending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => filter.SortDescending
                ? query.OrderByDescending(o => o.OrderDate)
                : query.OrderBy(o => o.OrderDate)
        };

        // Apply pagination
        var orders = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(o => o.Items)
            .Select(o => new AdminOrderListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerEmail = o.UserEmail,
                CustomerName = o.DeliveryAddress, // Could be parsed or use a separate name field
                IsGuestOrder = o.IsGuestOrder,
                Total = o.Total,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus ?? "Unknown",
                OrderDate = o.OrderDate,
                ItemCount = o.Items.Count
            })
            .ToListAsync(ct);

        return (orders, totalCount);
    }

    public async Task<OrderStatsDto> GetOrderStatsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var allOrders = await _context.Orders.ToListAsync(ct);
        var completedOrders = allOrders.Where(o => o.Status == "Delivered" || o.PaymentStatus == "Paid");

        return new OrderStatsDto
        {
            TotalOrders = allOrders.Count,
            PendingOrders = allOrders.Count(o => o.Status == "Pending"),
            ProcessingOrders = allOrders.Count(o => o.Status == "Processing"),
            DeliveredOrders = allOrders.Count(o => o.Status == "Delivered"),
            CancelledOrders = allOrders.Count(o => o.Status == "Cancelled"),
            TotalRevenue = completedOrders.Sum(o => o.Total),
            TodayRevenue = completedOrders.Where(o => o.OrderDate.Date == today).Sum(o => o.Total),
            WeekRevenue = completedOrders.Where(o => o.OrderDate >= weekAgo).Sum(o => o.Total),
            MonthRevenue = completedOrders.Where(o => o.OrderDate >= monthAgo).Sum(o => o.Total)
        };
    }

    public async Task<List<AdminOrderListDto>> GetRecentOrdersAsync(int count = 5, CancellationToken ct = default)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync(ct);

        return orders.Select(o => new AdminOrderListDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerEmail = o.UserEmail,
            CustomerName = null, // Or parse from DeliveryAddress if needed
            IsGuestOrder = o.IsGuestOrder,
            Total = o.Total,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            OrderDate = o.OrderDate,
            ItemCount = o.Items.Count
        }).ToList();
    }

}