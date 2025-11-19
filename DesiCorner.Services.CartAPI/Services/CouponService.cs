using DesiCorner.Contracts.Coupons;

namespace DesiCorner.Services.CartAPI.Services;

public class CouponService : ICouponService
{
    private readonly ILogger<CouponService> _logger;

    // In-memory coupons for now. Later we'll integrate with Coupon microservice
    private readonly List<CouponDto> _coupons = new()
    {
        new CouponDto
        {
            Id = Guid.NewGuid(),
            Code = "WELCOME10",
            DiscountAmount = 10,
            DiscountType = "Fixed",
            MinAmount = 0,
            IsActive = true,
            MaxUsageCount = 1000,
            UsedCount = 0,
            ExpiryDate = DateTime.UtcNow.AddMonths(3),
            CreatedAt = DateTime.UtcNow
        },
        new CouponDto
        {
            Id = Guid.NewGuid(),
            Code = "SAVE20",
            DiscountAmount = 20,
            DiscountType = "Fixed",
            MinAmount = 100,
            IsActive = true,
            MaxUsageCount = 500,
            UsedCount = 0,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            CreatedAt = DateTime.UtcNow
        },
        new CouponDto
        {
            Id = Guid.NewGuid(),
            Code = "PERCENT15",
            DiscountAmount = 15,
            DiscountType = "Percentage",
            MinAmount = 50,
            MaxDiscount = 50,
            IsActive = true,
            MaxUsageCount = 1000,
            UsedCount = 0,
            ExpiryDate = DateTime.UtcNow.AddMonths(2),
            CreatedAt = DateTime.UtcNow
        },
        new CouponDto
        {
            Id = Guid.NewGuid(),
            Code = "FEAST50",
            DiscountAmount = 50,
            DiscountType = "Fixed",
            MinAmount = 200,
            IsActive = true,
            MaxUsageCount = 100,
            UsedCount = 0,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        }
    };

    public CouponService(ILogger<CouponService> logger)
    {
        _logger = logger;
    }

    public Task<ValidateCouponResponseDto> ValidateCouponAsync(ValidateCouponRequestDto request, CancellationToken ct = default)
    {
        var coupon = _coupons.FirstOrDefault(c =>
            c.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase));

        if (coupon == null)
        {
            return Task.FromResult(new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "Invalid coupon code",
                DiscountAmount = 0
            });
        }

        // Check if active
        if (!coupon.IsActive)
        {
            return Task.FromResult(new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "This coupon is no longer active",
                DiscountAmount = 0
            });
        }

        // Check expiry
        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
        {
            return Task.FromResult(new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "This coupon has expired",
                DiscountAmount = 0
            });
        }

        // Check usage count
        if (coupon.UsedCount >= coupon.MaxUsageCount)
        {
            return Task.FromResult(new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = "This coupon has reached its maximum usage limit",
                DiscountAmount = 0
            });
        }

        // Check minimum amount
        if (request.CartTotal < coupon.MinAmount)
        {
            return Task.FromResult(new ValidateCouponResponseDto
            {
                IsValid = false,
                Message = $"Minimum cart total of ${coupon.MinAmount} required for this coupon",
                DiscountAmount = 0
            });
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

        return Task.FromResult(new ValidateCouponResponseDto
        {
            IsValid = true,
            Message = "Coupon applied successfully",
            DiscountAmount = discountAmount,
            Coupon = coupon
        });
    }
}