using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Services.PaymentAPI.Models;

/// <summary>
/// A Stripe webhook event that has already been applied. Stripe delivers events
/// at least once, so the event id is the de-duplication key.
/// </summary>
public class ProcessedStripeEvent
{
    /// <summary>
    /// Stripe event ID (e.g., evt_xxx)
    /// </summary>
    [Key]
    [MaxLength(255)]
    public string EventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
