using System.Security.Claims;

namespace DesiCorner.Gateway.Transforms;

public static class ForwardingTransforms
{
    /// <summary>
    /// Headers that carry caller identity to downstream services. Only the gateway may set
    /// them, and only from a validated token, so any inbound copy is removed first.
    /// </summary>
    public static readonly string[] IdentityHeaders =
    {
        "X-Forwarded-UserId",
        "X-Forwarded-Roles",
        "X-Forwarded-Email",
        "X-Forwarded-Phone",
        "X-Auth-Source"
    };

    public static void StripInboundIdentityHeaders(HttpContext ctx)
    {
        foreach (var header in IdentityHeaders)
        {
            ctx.Request.Headers.Remove(header);
        }
    }

    public static void AddForwardedIdentityHeaders(HttpContext ctx)
    {
        var user = ctx.User;
        if (!user.Identity?.IsAuthenticated ?? true) return;

        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value);

        // Extract email and phone from JWT claims
        var email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var phone = user.FindFirst("phone_number")?.Value ?? user.FindFirst(ClaimTypes.MobilePhone)?.Value ?? "";

        ctx.Request.Headers["X-Request-Id"] = ctx.TraceIdentifier;
        ctx.Request.Headers["X-Forwarded-UserId"] = sub;
        ctx.Request.Headers["X-Forwarded-Roles"] = string.Join(",", roles);
        ctx.Request.Headers["X-Forwarded-Email"] = email;
        ctx.Request.Headers["X-Forwarded-Phone"] = phone;

        if (!ctx.Request.Headers.ContainsKey("X-Auth-Source"))
            ctx.Request.Headers["X-Auth-Source"] = "jwt";
    }
}