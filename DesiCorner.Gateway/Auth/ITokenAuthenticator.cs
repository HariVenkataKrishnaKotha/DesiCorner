using System.Security.Claims;

namespace DesiCorner.Gateway.Auth;

public interface ITokenAuthenticator
{
    Task<(bool ok, ClaimsPrincipal? principal, string source, string? error)> AuthenticateAsync(string bearerToken, CancellationToken ct);
}
