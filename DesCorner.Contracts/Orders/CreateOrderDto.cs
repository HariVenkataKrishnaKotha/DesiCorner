using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Orders;

public class CreateOrderDto
{
    // Guest checkout fields
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? OtpCode { get; set; }
    public string? SessionId { get; set; }

    // Order type
    [Required]
    public string OrderType { get; set; } = "Delivery"; // "Delivery" or "Pickup"

    public DateTime? ScheduledPickupTime { get; set; }

    // Delivery address (required only for Delivery orders)
    public string? DeliveryAddress { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryState { get; set; }
    public string? DeliveryZipCode { get; set; }
    public string? DeliveryInstructions { get; set; }

    // Payment
    [Required]
    public string PaymentMethod { get; set; } = "Stripe"; // "Stripe" or "PayAtPickup"

    public string? PaymentIntentId { get; set; } // Required only for Stripe
}