namespace DesiCorner.Services.CartAPI.Models;

public class Coupon
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = "Fixed"; // Fixed or Percentage
    public decimal MinAmount { get; set; }
    public decimal? MaxDiscount { get; set; } // For percentage coupons
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxUsageCount { get; set; } = 1000;
    public int UsedCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; } // Admin who created
}