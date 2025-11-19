using DesiCorner.Contracts.Cart;
using DesiCorner.Contracts.Common;
using DesiCorner.Services.CartAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DesiCorner.Services.CartAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Get cart for current user or session
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCart([FromQuery] string? sessionId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();

            if (!userId.HasValue && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Either user must be authenticated or sessionId must be provided"
                });
            }

            var cart = await _cartService.GetCartAsync(userId, sessionId, ct);

            if (cart == null)
            {
                // Return empty cart
                return Ok(new ResponseDto
                {
                    IsSuccess = true,
                    Result = new CartDto
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        SessionId = sessionId,
                        Items = new List<CartItemDto>(),
                        SubTotal = 0,
                        TaxAmount = 0,
                        DeliveryFee = 0,
                        Total = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                });
            }

            var cartDto = MapToDto(cart);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = cartDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving cart"
            });
        }
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto request, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();

            if (!userId.HasValue && string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Either user must be authenticated or sessionId must be provided"
                });
            }

            var cart = await _cartService.AddToCartAsync(
                userId ?? request.UserId,
                request.SessionId,
                request.ProductId,
                request.Quantity,
                ct);

            var cartDto = MapToDto(cart);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Item added to cart successfully",
                Result = cartDto
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
            _logger.LogError(ex, "Error adding to cart");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error adding item to cart"
            });
        }
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("update")]
    public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto request, CancellationToken ct)
    {
        try
        {
            var cart = await _cartService.UpdateCartItemAsync(request.CartItemId, request.Quantity, ct);
            var cartDto = MapToDto(cart);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Cart item updated successfully",
                Result = cartDto
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
            _logger.LogError(ex, "Error updating cart item");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error updating cart item"
            });
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("item/{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(Guid cartItemId, CancellationToken ct)
    {
        try
        {
            var result = await _cartService.RemoveFromCartAsync(cartItemId, ct);

            if (!result)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Cart item not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Item removed from cart successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error removing item from cart"
            });
        }
    }

    /// <summary>
    /// Clear entire cart
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart([FromQuery] string? sessionId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();

            if (!userId.HasValue && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Either user must be authenticated or sessionId must be provided"
                });
            }

            await _cartService.ClearCartAsync(userId, sessionId, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Cart cleared successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error clearing cart"
            });
        }
    }

    /// <summary>
    /// Apply coupon code
    /// </summary>
    [HttpPost("apply-coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponDto request, CancellationToken ct)
    {
        try
        {
            var cart = await _cartService.ApplyCouponAsync(request.CartId, request.CouponCode, ct);
            var cartDto = MapToDto(cart);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Coupon applied successfully",
                Result = cartDto
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
            _logger.LogError(ex, "Error applying coupon");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error applying coupon"
            });
        }
    }

    /// <summary>
    /// Remove coupon code
    /// </summary>
    [HttpPost("remove-coupon/{cartId}")]
    public async Task<IActionResult> RemoveCoupon(Guid cartId, CancellationToken ct)
    {
        try
        {
            var cart = await _cartService.RemoveCouponAsync(cartId, ct);
            var cartDto = MapToDto(cart);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Coupon removed successfully",
                Result = cartDto
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
            _logger.LogError(ex, "Error removing coupon");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error removing coupon"
            });
        }
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static CartDto MapToDto(Models.Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            SessionId = cart.SessionId,
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImage = i.ProductImage,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList(),
            CouponCode = cart.CouponCode,
            DiscountAmount = cart.DiscountAmount,
            SubTotal = cart.SubTotal,
            TaxAmount = cart.TaxAmount,
            DeliveryFee = cart.DeliveryFee,
            Total = cart.Total,
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt
        };
    }
}