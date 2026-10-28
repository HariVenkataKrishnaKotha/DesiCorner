using DesiCorner.Services.PaymentAPI.Models;

namespace DesiCorner.Services.PaymentAPI.Services;

/// <summary>
/// The caller a payment belongs to: a signed-in user (JWT sub), otherwise a guest (X-Session-Id).
/// </summary>
public sealed record PaymentOwner(Guid? UserId, string? GuestSessionId)
{
    public static PaymentOwner FromRequest(HttpContext context)
    {
        Guid? userId = Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? id : null;
        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
        return new PaymentOwner(userId, userId.HasValue || string.IsNullOrEmpty(sessionId) ? null : sessionId);
    }

    /// <summary>
    /// True when this caller created the payment. A user payment is owned by that user only;
    /// a guest payment only by the guest session that created it.
    /// </summary>
    public bool Owns(Payment payment) =>
        payment.UserId.HasValue
            ? payment.UserId == UserId
            : !string.IsNullOrEmpty(GuestSessionId) &&
              string.Equals(payment.GuestSessionId, GuestSessionId, StringComparison.Ordinal);
}
