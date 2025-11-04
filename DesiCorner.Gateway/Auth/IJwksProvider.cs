using Microsoft.IdentityModel.Tokens;

namespace DesiCorner.Gateway.Auth;

public interface IJwksProvider
{
    Task<JsonWebKeySet> GetAsync(CancellationToken ct);
    Task InvalidateAsync(CancellationToken ct);
}