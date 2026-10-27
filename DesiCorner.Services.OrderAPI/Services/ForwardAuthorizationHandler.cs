using System.Net.Http.Headers;

namespace DesiCorner.Services.OrderAPI.Services;

/// <summary>
/// Forwards the authenticated caller's bearer token on outgoing service-to-service calls,
/// so the downstream service (via the gateway) identifies the user from a token it can
/// validate rather than from a header anyone could set.
/// </summary>
public class ForwardAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (request.Headers.Authorization == null &&
            httpContext?.User.Identity?.IsAuthenticated == true &&
            AuthenticationHeaderValue.TryParse(httpContext.Request.Headers.Authorization.ToString(), out var authorization) &&
            string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = authorization;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
