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
[Authorize]
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
    /// Create a new order
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User must be authenticated to create an order"
                });
            }

            var userEmail = GetUserEmail() ?? "";
            var userPhone = GetUserPhone() ?? "";

            var order = await _orderService.CreateOrderAsync(userId.Value, userEmail, userPhone, request, ct);
            var orderDto = MapToDto(order);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Order created successfully",
                Result = orderDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while creating the order"
            });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByIdAsync(orderId, ct);

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