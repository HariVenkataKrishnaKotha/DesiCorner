namespace DesiCorner.Services.CartAPI.Models;

/// <summary>Outcome of merging a guest cart into a user's cart.</summary>
/// <param name="Cart">The user's cart after the merge, or null if the user has no cart.</param>
/// <param name="DroppedItems">Guest lines that were not carried over.</param>
/// <param name="DroppedCouponCode">The guest coupon, if it was not carried over.</param>
public sealed record CartMergeResult(Cart? Cart, IReadOnlyList<DroppedCartItem> DroppedItems, string? DroppedCouponCode);

public sealed record DroppedCartItem(Guid ProductId, string ProductName, int Quantity, string Reason)
{
    public const string NotFound = "not_found";
    public const string Unavailable = "unavailable";
}
