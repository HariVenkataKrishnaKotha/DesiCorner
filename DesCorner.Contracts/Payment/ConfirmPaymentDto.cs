using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Payment;

public class ConfirmPaymentDto
{
    [Required]
    public string PaymentIntentId { get; set; } = string.Empty;

    [Required]
    public Guid OrderId { get; set; }
}
