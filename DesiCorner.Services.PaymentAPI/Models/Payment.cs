using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DesiCorner.Services.PaymentAPI.Models;

public class Payment
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Stripe Payment Intent ID (e.g., pi_xxx)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// User ID (null for guest orders)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User email (required for Stripe receipt)
    /// </summary>
    [MaxLength(255)]
    public string? UserEmail { get; set; }

    /// <summary>
    /// Amount in smallest currency unit (cents for USD)
    /// Stripe uses long for amounts (e.g., $10.50 = 1050 cents)
    /// </summary>
    public long AmountInCents { get; set; }

    /// <summary>
    /// Amount in decimal (for display, calculated from AmountInCents)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., usd, eur)
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Payment status from Stripe:
    /// requires_payment_method, requires_confirmation, requires_action, 
    /// processing, requires_capture, canceled, succeeded
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "requires_payment_method";

    /// <summary>
    /// Payment method ID (e.g., pm_xxx) after customer provides card details
    /// </summary>
    [MaxLength(255)]
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// Charge ID (e.g., ch_xxx) created after successful payment
    /// </summary>
    [MaxLength(255)]
    public string? ChargeId { get; set; }

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Last payment error code from Stripe
    /// </summary>
    [MaxLength(100)]
    public string? LastPaymentErrorCode { get; set; }

    /// <summary>
    /// Order ID this payment is for
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Stripe client secret (used by frontend to confirm payment)
    /// </summary>
    [MaxLength(500)]
    public string? ClientSecret { get; set; }

    // Audit timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}