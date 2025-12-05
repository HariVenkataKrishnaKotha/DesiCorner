using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Payment;

public class VerifyPaymentRequest
{
    [Required]
    public string PaymentIntentId { get; set; } = string.Empty;
}