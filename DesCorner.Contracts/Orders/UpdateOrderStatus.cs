using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Orders;

public class UpdateOrderStatusDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
