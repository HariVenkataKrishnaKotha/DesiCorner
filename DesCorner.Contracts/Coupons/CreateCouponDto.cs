using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Coupons;

public class CreateCouponDto
{
    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public decimal DiscountAmount { get; set; }

    [Required]
    public string DiscountType { get; set; } = "Fixed"; // Fixed or Percentage

    [Range(0, 10000)]
    public decimal MinAmount { get; set; }

    public decimal? MaxDiscount { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, 10000)]
    public int MaxUsageCount { get; set; } = 1000;
}
