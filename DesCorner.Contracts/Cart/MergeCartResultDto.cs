namespace DesiCorner.Contracts.Cart;

/// <summary>
/// Result of merging a guest cart into the signed-in user's cart (POST /api/cart/merge).
/// </summary>
public class MergeCartResultDto
{
    /// <summary>The user's cart after the merge.</summary>
    public CartDto Cart { get; set; } = new();

    /// <summary>Guest cart lines that were not carried over.</summary>
    public List<DroppedCartItemDto> DroppedItems { get; set; } = new();

    /// <summary>The guest cart's coupon, when it was not carried over because it is no longer valid.</summary>
    public string? DroppedCouponCode { get; set; }
}

public class DroppedCartItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    /// <summary>"not_found" (the product no longer exists) or "unavailable".</summary>
    public string Reason { get; set; } = string.Empty;
}
