using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DesiCorner.Services.OrderAPI.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public bool IsGuestOrder { get; set; }

    [Required]
    [MaxLength(255)]
    public string UserEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string UserPhone { get; set; } = string.Empty;

    // Navigation property
    public List<OrderItem> Items { get; set; } = new();

    // Delivery Address
    [Required]
    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DeliveryCity { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DeliveryState { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string DeliveryZipCode { get; set; } = string.Empty;

    // Pricing
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DeliveryFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [MaxLength(50)]
    public string? CouponCode { get; set; }

    // Status
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    // Payment
    [MaxLength(255)]
    public string? PaymentIntentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentStatus { get; set; } = "Pending";

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Stripe";

    // Timestamps
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedDeliveryTime { get; set; }
    public DateTime? DeliveredAt { get; set; }

    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}