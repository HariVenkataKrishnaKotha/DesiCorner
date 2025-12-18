using DesiCorner.Contracts.Coupons;
using DesiCorner.Services.CartAPI.Data;
using DesiCorner.Services.CartAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.CartAPI.Services;

public class CouponService : ICouponService
{
    private readonly CartDbContext _context;
    private readonly ILogger<CouponService> _logger;

    public CouponService(CartDbContext context, ILogger<CouponService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ValidateCouponResponseDto> ValidateCouponAsync(ValidateCouponRequestDto request, CancellationToken ct = default)
    {
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToLower() == request.Code.ToLower(), ct);

        if (coupon == null)
        {
            return new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "Invalid coupon code",
                DiscountAmount = 0
            };
        }

        // Check if active
        if (!coupon.IsActive)
        {
            return new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "This coupon is no longer active",
                DiscountAmount = 0
            };
        }

        // Check expiry
        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
        {
            return new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "This coupon has expired",
                DiscountAmount = 0
            };
        }

        // Check usage count
        if (coupon.UsedCount >= coupon.MaxUsageCount)
        {
            return new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "This coupon has reached its maximum usage limit",
                DiscountAmount = 0
            };
        }

        // Check minimum amount
        if (request.CartTotal < coupon.MinAmount)
        {
            return new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = $"Minimum cart total of ${coupon.MinAmount} required for this coupon",
                DiscountAmount = 0
            };
        }

        // Calculate discount
        decimal discountAmount;
        if (coupon.DiscountType == "Percentage")
        {
            discountAmount = request.CartTotal * (coupon.DiscountAmount / 100);

            // Apply max discount cap if exists
            if (coupon.MaxDiscount.HasValue && discountAmount > coupon.MaxDiscount.Value)
            {
                discountAmount = coupon.MaxDiscount.Value;
            }
        }
        else
        {
            // Fixed discount
            discountAmount = coupon.DiscountAmount;
        }

        _logger.LogInformation("Coupon {Code} validated successfully. Discount: ${Discount}", coupon.Code, discountAmount);

        return new ValidateCouponResponseDto
        {
            IsValid = true,
            Message = "Coupon applied successfully",
            DiscountAmount = discountAmount,
            Coupon = MapToDto(coupon)
        };
    }

    public async Task<List<CouponDto>> GetAllCouponsAsync(CouponFilterDto filter, CancellationToken ct = default)
    {
        var query = _context.Coupons.AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(term) ||
                                     (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == filter.IsActive.Value);
        }

        if (filter.IsExpired.HasValue)
        {
            if (filter.IsExpired.Value)
            {
                query = query.Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < DateTime.UtcNow);
            }
            else
            {
                query = query.Where(c => !c.ExpiryDate.HasValue || c.ExpiryDate.Value >= DateTime.UtcNow);
            }
        }

        var coupons = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return coupons.Select(MapToDto).ToList();
    }

    public async Task<CouponDto?> GetCouponByIdAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await _context.Coupons.FindAsync(new object[] { id }, ct);
        return coupon == null ? null : MapToDto(coupon);
    }

    public async Task<CouponDto?> GetCouponByCodeAsync(string code, CancellationToken ct = default)
    {
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower(), ct);
        return coupon == null ? null : MapToDto(coupon);
    }

    public async Task<CouponDto> CreateCouponAsync(CreateCouponDto dto, string createdBy, CancellationToken ct = default)
    {
        // Check if code already exists
        var existing = await _context.Coupons
            .AnyAsync(c => c.Code.ToLower() == dto.Code.ToLower(), ct);

        if (existing)
        {
            throw new InvalidOperationException($"Coupon code '{dto.Code}' already exists");
        }

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = dto.Code.ToUpper(),
            Description = dto.Description,
            DiscountAmount = dto.DiscountAmount,
            DiscountType = dto.DiscountType,
            MinAmount = dto.MinAmount,
            MaxDiscount = dto.MaxDiscount,
            ExpiryDate = dto.ExpiryDate,
            IsActive = dto.IsActive,
            MaxUsageCount = dto.MaxUsageCount,
            UsedCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Coupon {Code} created by {Admin}", coupon.Code, createdBy);

        return MapToDto(coupon);
    }

    public async Task<CouponDto?> UpdateCouponAsync(UpdateCouponDto dto, CancellationToken ct = default)
    {
        var coupon = await _context.Coupons.FindAsync(new object[] { dto.Id }, ct);

        if (coupon == null)
        {
            return null;
        }

        // Check if new code conflicts with existing
        if (!coupon.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase))
        {
            var codeExists = await _context.Coupons
                .AnyAsync(c => c.Id != dto.Id && c.Code.ToLower() == dto.Code.ToLower(), ct);

            if (codeExists)
            {
                throw new InvalidOperationException($"Coupon code '{dto.Code}' already exists");
            }
        }

        coupon.Code = dto.Code.ToUpper();
        coupon.Description = dto.Description;
        coupon.DiscountAmount = dto.DiscountAmount;
        coupon.DiscountType = dto.DiscountType;
        coupon.MinAmount = dto.MinAmount;
        coupon.MaxDiscount = dto.MaxDiscount;
        coupon.ExpiryDate = dto.ExpiryDate;
        coupon.IsActive = dto.IsActive;
        coupon.MaxUsageCount = dto.MaxUsageCount;
        coupon.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Coupon {Code} updated", coupon.Code);

        return MapToDto(coupon);
    }

    public async Task<bool> DeleteCouponAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await _context.Coupons.FindAsync(new object[] { id }, ct);

        if (coupon == null)
        {
            return false;
        }

        _context.Coupons.Remove(coupon);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Coupon {Code} deleted", coupon.Code);

        return true;
    }

    public async Task<bool> ToggleCouponStatusAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await _context.Coupons.FindAsync(new object[] { id }, ct);

        if (coupon == null)
        {
            return false;
        }

        coupon.IsActive = !coupon.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Coupon {Code} status toggled to {Status}", coupon.Code, coupon.IsActive);

        return true;
    }

    public async Task IncrementUsageAsync(string code, CancellationToken ct = default)
    {
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower(), ct);

        if (coupon != null)
        {
            coupon.UsedCount++;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<CouponStatsDto> GetCouponStatsAsync(CancellationToken ct = default)
    {
        var coupons = await _context.Coupons.ToListAsync(ct);
        var now = DateTime.UtcNow;

        return new CouponStatsDto
        {
            TotalCoupons = coupons.Count,
            ActiveCoupons = coupons.Count(c => c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate.Value >= now)),
            ExpiredCoupons = coupons.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < now),
            FullyUsedCoupons = coupons.Count(c => c.UsedCount >= c.MaxUsageCount),
            TotalDiscountGiven = 0 // Would need order data to calculate this properly
        };
    }

    private static CouponDto MapToDto(Coupon coupon)
    {
        return new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountAmount = coupon.DiscountAmount,
            DiscountType = coupon.DiscountType,
            MinAmount = coupon.MinAmount,
            MaxDiscount = coupon.MaxDiscount,
            ExpiryDate = coupon.ExpiryDate,
            IsActive = coupon.IsActive,
            MaxUsageCount = coupon.MaxUsageCount,
            UsedCount = coupon.UsedCount,
            CreatedAt = coupon.CreatedAt
        };
    }
}