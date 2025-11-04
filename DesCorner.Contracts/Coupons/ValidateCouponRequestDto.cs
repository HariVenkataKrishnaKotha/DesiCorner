using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Coupons;

public class ValidateCouponRequestDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 100000)]
    public decimal CartTotal { get; set; }

    public Guid? UserId { get; set; }
}
