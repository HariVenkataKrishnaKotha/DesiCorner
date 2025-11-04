using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Payment;

public class PaymentIntentRequestDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [Range(0.50, 1000000)] // Stripe minimum is $0.50
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "usd";

    public Dictionary<string, string>? Metadata { get; set; }
}
