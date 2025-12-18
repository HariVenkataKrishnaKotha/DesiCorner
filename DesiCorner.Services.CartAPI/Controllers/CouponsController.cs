using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Coupons;
using DesiCorner.Services.CartAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesiCorner.Services.CartAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;
    private readonly ILogger<CouponsController> _logger;

    public CouponsController(ICouponService couponService, ILogger<CouponsController> logger)
    {
        _couponService = couponService;
        _logger = logger;
    }

    /// <summary>
    /// Get all coupons (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> GetAllCoupons([FromQuery] CouponFilterDto filter, CancellationToken ct)
    {
        try
        {
            var coupons = await _couponService.GetAllCouponsAsync(filter, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = coupons
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coupons");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to retrieve coupons"
            });
        }
    }

    /// <summary>
    /// Get coupon by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> GetCouponById(Guid id, CancellationToken ct)
    {
        var coupon = await _couponService.GetCouponByIdAsync(id, ct);

        if (coupon == null)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "Coupon not found"
            });
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = coupon
        });
    }

    /// <summary>
    /// Create a new coupon (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> CreateCoupon([FromBody] CreateCouponDto dto, CancellationToken ct)
    {
        try
        {
            var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown";
            var coupon = await _couponService.CreateCouponAsync(dto, adminEmail, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Coupon created successfully",
                Result = coupon
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
            _logger.LogError(ex, "Error creating coupon");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to create coupon"
            });
        }
    }

    /// <summary>
    /// Update a coupon (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> UpdateCoupon(Guid id, [FromBody] UpdateCouponDto dto, CancellationToken ct)
    {
        if (id != dto.Id)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = "ID mismatch"
            });
        }

        try
        {
            var coupon = await _couponService.UpdateCouponAsync(dto, ct);

            if (coupon == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Coupon not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Coupon updated successfully",
                Result = coupon
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
            _logger.LogError(ex, "Error updating coupon");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to update coupon"
            });
        }
    }

    /// <summary>
    /// Delete a coupon (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> DeleteCoupon(Guid id, CancellationToken ct)
    {
        var result = await _couponService.DeleteCouponAsync(id, ct);

        if (!result)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "Coupon not found"
            });
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Coupon deleted successfully"
        });
    }

    /// <summary>
    /// Toggle coupon active status (Admin only)
    /// </summary>
    [HttpPost("{id}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> ToggleCouponStatus(Guid id, CancellationToken ct)
    {
        var result = await _couponService.ToggleCouponStatusAsync(id, ct);

        if (!result)
        {
            return NotFound(new ResponseDto
            {
                IsSuccess = false,
                Message = "Coupon not found"
            });
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Message = "Coupon status toggled successfully"
        });
    }

    /// <summary>
    /// Get coupon statistics (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ResponseDto>> GetCouponStats(CancellationToken ct)
    {
        try
        {
            var stats = await _couponService.GetCouponStatsAsync(ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coupon stats");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Failed to retrieve coupon statistics"
            });
        }
    }
}