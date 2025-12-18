namespace DesiCorner.Contracts.Coupons;

public class CreateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = "Fixed";
    public decimal MinAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxUsageCount { get; set; } = 1000;
}

public class UpdateCouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = "Fixed";
    public decimal MinAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int MaxUsageCount { get; set; }
}

public class CouponFilterDto
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsExpired { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CouponStatsDto
{
    public int TotalCoupons { get; set; }
    public int ActiveCoupons { get; set; }
    public int ExpiredCoupons { get; set; }
    public int FullyUsedCoupons { get; set; }
    public decimal TotalDiscountGiven { get; set; }
}