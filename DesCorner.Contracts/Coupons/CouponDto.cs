using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.Contracts.Coupons;

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = "Fixed"; // Fixed or Percentage
    public decimal MinAmount { get; set; }
    public decimal? MaxDiscount { get; set; } // For percentage coupons
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int MaxUsageCount { get; set; }
    public int UsedCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
