using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Cart;

public class ApplyCouponDto
{
    [Required]
    public Guid CartId { get; set; }

    [Required]
    public string CouponCode { get; set; } = string.Empty;
}
