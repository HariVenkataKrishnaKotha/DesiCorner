using DesiCorner.Services.PaymentAPI.Models;

namespace DesiCorner.Services.PaymentAPI.Services;

/// <summary>
/// Who is calling: the signed-in user (JWT sub), and the guest session (X-Session-Id) if one is sent.
/// A payment belongs to its user if it has one, otherwise to the guest session that created it.
/// </summary>
public sealed record PaymentOwner(Guid? UserId, string? GuestSessionId)
{
    public static PaymentOwner FromRequest(HttpContext context)
    {
        Guid? userId = Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? id : null;
        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
        return new PaymentOwner(userId, string.IsNullOrEmpty(sessionId) ? null : sessionId);
    }

    /// <summary>
    /// True when this caller created the payment. A user payment is owned by that user only;
    /// a guest payment only by the guest session that created it, even if that guest has since
    /// signed in (for example in another tab) and now also sends a bearer token.
    /// </summary>
    public bool Owns(Payment payment) =>
        payment.UserId.HasValue
            ? payment.UserId == UserId
            : !string.IsNullOrEmpty(GuestSessionId) &&
              string.Equals(payment.GuestSessionId, GuestSessionId, StringComparison.Ordinal);
}
