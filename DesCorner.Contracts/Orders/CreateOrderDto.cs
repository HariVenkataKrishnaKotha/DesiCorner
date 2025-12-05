using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Orders;

public class CreateOrderDto
{
    // Guest checkout fields (optional - for non-authenticated users)
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? OtpCode { get; set; }
    public string? SessionId { get; set; }

    // Delivery address fields (required)
    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    public string DeliveryCity { get; set; } = string.Empty;

    [Required]
    public string DeliveryState { get; set; } = string.Empty;

    [Required]
    public string DeliveryZipCode { get; set; } = string.Empty;

    public string? DeliveryInstructions { get; set; }

    // Payment
    public string PaymentMethod { get; set; } = "CashOnDelivery";

    [Required(ErrorMessage = "Payment Intent ID is required")]
    public string? PaymentIntentId { get; set; }
}