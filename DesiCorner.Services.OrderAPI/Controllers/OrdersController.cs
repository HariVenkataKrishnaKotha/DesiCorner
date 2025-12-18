using DesCorner.Contracts.Orders;
using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Orders;
using DesiCorner.Services.OrderAPI.Models;
using DesiCorner.Services.OrderAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace DesiCorner.Services.OrderAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order (supports both authenticated and guest checkout)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();

            // For authenticated users, get email and phone from forwarded headers
            string? email = request.Email;
            string? phone = request.Phone;

            if (userId.HasValue)
            {
                // Override with forwarded headers for authenticated users
                email = Request.Headers["X-Forwarded-Email"].FirstOrDefault() ?? email;
                phone = Request.Headers["X-Forwarded-Phone"].FirstOrDefault() ?? phone;
            }

            var order = await _orderService.CreateOrderAsync(userId?.ToString(), request, email, phone, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Order placed successfully",
                Result = new OrderDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = order.UserId,
                    UserEmail = order.UserEmail,
                    UserPhone = order.UserPhone,
                    Status = order.Status.ToString(),
                    SubTotal = order.SubTotal,
                    TaxAmount = order.TaxAmount,
                    DeliveryFee = order.DeliveryFee,
                    Total = order.Total,
                    DeliveryAddress = order.DeliveryAddress,
                    DeliveryCity = order.DeliveryCity,
                    DeliveryState = order.DeliveryState,
                    DeliveryZipCode = order.DeliveryZipCode,
                    Items = order.Items.Select(i => new OrderItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity
                    }).ToList(),
                    CreatedAt = order.CreatedAt
                }
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error creating order"
            });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [AllowAnonymous]  // Allow guests to view their order briefly after creation
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(orderId, ct);

            if (order == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Order not found"
                });
            }

            var userId = GetUserId();

            // Authorization logic
            if (userId.HasValue)
            {
                // Authenticated user - check ownership (unless admin)
                if (!IsAdmin() && order.UserId != userId)
                {
                    return Forbid();
                }
            }
            else
            {
                // Guest user - only allow viewing within 5 minutes of order creation
                // This allows guests to see their confirmation page immediately
                var orderAge = DateTime.UtcNow - order.OrderDate;
                if (orderAge > TimeSpan.FromMinutes(5))
                {
                    return Unauthorized(new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "Please login to view order history. Guest orders are only viewable for 5 minutes after creation."
                    });
                }
            }

            var orderDto = MapToDto(order);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = orderDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", orderId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the order"
            });
        }
    }

    /// <summary>
    /// Get order by order number
    /// </summary>
    [Authorize]
    [HttpGet("number/{orderNumber}")]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByNumberAsync(orderNumber, ct);

            if (order == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Order not found"
                });
            }

            // Check if user owns this order (unless admin)
            if (!IsAdmin() && order.UserId != userId)
            {
                return Forbid();
            }

            var orderDto = MapToDto(order);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = orderDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order by number {OrderNumber}", orderNumber);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the order"
            });
        }
    }

    /// <summary>
    /// Get current user's orders
    /// </summary>
    [Authorize]
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User must be authenticated"
                });
            }

            var orders = await _orderService.GetUserOrdersAsync(userId.Value, page, pageSize, ct);
            var totalCount = await _orderService.GetUserOrderCountAsync(userId.Value, ct);

            var orderDtos = orders.Select(MapToSummaryDto).ToList();

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = new
                {
                    Items = orderDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user orders");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving orders"
            });
        }
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    [HttpPut("status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDto request, CancellationToken ct)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(request.OrderId, request.Status, request.Notes, ct);
            var orderDto = MapToDto(order);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Order status updated successfully",
                Result = orderDto
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "Order not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while updating order status"
            });
        }
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [Authorize]
    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User must be authenticated"
                });
            }

            var result = await _orderService.CancelOrderAsync(orderId, userId.Value, ct);

            if (!result)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Order not found or you don't have permission to cancel it"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Order cancelled successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while cancelling the order"
            });
        }
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> GetAllOrders([FromQuery] AdminOrderFilterDto filter, CancellationToken ct)
    {
        try
        {
            var (orders, totalCount) = await _orderService.GetAllOrdersAsync(filter, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = new
                {
                    Orders = orders,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all orders");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to retrieve orders"
            });
        }
    }

    /// <summary>
    /// Get order statistics (Admin only)
    /// </summary>
    [HttpGet("admin/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> GetOrderStats(CancellationToken ct)
    {
        try
        {
            var stats = await _orderService.GetOrderStatsAsync(ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order stats");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to retrieve order statistics"
            });
        }
    }

    /// <summary>
    /// Get recent orders (Admin only)
    /// </summary>
    [HttpGet("admin/recent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRecentOrders([FromQuery] int count = 5, CancellationToken ct = default)
    {
        var orders = await _orderService.GetRecentOrdersAsync(count, ct);

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = orders
        });
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string? GetUserPhone()
    {
        return User.FindFirst("phone_number")?.Value;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            UserEmail = order.UserEmail,
            UserPhone = order.UserPhone,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImage = i.ProductImage,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList(),
            DeliveryAddress = order.DeliveryAddress,
            DeliveryCity = order.DeliveryCity,
            DeliveryState = order.DeliveryState,
            DeliveryZipCode = order.DeliveryZipCode,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            DeliveryFee = order.DeliveryFee,
            DiscountAmount = order.DiscountAmount,
            Total = order.Total,
            CouponCode = order.CouponCode,
            Status = order.Status,
            PaymentIntentId = order.PaymentIntentId,
            PaymentStatus = order.PaymentStatus,
            OrderDate = order.OrderDate,
            EstimatedDeliveryTime = order.EstimatedDeliveryTime,
            DeliveredAt = order.DeliveredAt,
            SpecialInstructions = order.SpecialInstructions
        };
    }

    private static OrderSummaryDto MapToSummaryDto(Order order)
    {
        return new OrderSummaryDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Total = order.Total,
            Status = order.Status,
            ItemCount = order.Items.Sum(i => i.Quantity)
        };
    }

}