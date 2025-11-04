using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Orders;

public class CreateOrderDto
{
    [Required]
    public Guid CartId { get; set; }

    [Required]
    public Guid DeliveryAddressId { get; set; }

    public string? SpecialInstructions { get; set; }

    // Payment will be handled separately
    public string? PaymentMethod { get; set; } = "Stripe";
}
