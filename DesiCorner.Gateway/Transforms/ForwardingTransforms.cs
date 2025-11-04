using System.Security.Claims;

namespace DesiCorner.Gateway.Transforms;

public static class ForwardingTransforms
{
    public static void AddForwardedIdentityHeaders(HttpContext ctx)
    {
        var user = ctx.User;
        if (!user.Identity?.IsAuthenticated ?? true) return;

        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value);

        ctx.Request.Headers["X-Request-Id"] = ctx.TraceIdentifier;
        ctx.Request.Headers["X-Forwarded-UserId"] = sub;
        ctx.Request.Headers["X-Forwarded-Roles"] = string.Join(",", roles);
        if (!ctx.Request.Headers.ContainsKey("X-Auth-Source"))
            ctx.Request.Headers["X-Auth-Source"] = "jwt";
    }
}