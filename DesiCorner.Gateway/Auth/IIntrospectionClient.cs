using System.Security.Claims;

namespace DesiCorner.Gateway.Auth;

public interface IIntrospectionClient
{
    Task<(bool active, ClaimsPrincipal? principal, string? error)> IntrospectAsync(string token, CancellationToken ct);
}