using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;

namespace DesiCorner.Gateway.Auth;

public sealed class TokenAuthenticator : ITokenAuthenticator
{
    private readonly IJwksProvider _jwks;
    private readonly IConfiguration _cfg;
    private readonly IIntrospectionClient _introspect;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TokenAuthenticator> _log;

    private readonly string _mode;
    private readonly string _issuer;
    private readonly string[] _audiences;
    private readonly TimeSpan _clockSkew;

    public TokenAuthenticator(
        IJwksProvider jwks,
        IConfiguration cfg,
        IIntrospectionClient introspect,
        IDistributedCache cache,
        ILogger<TokenAuthenticator> log)
    {
        _jwks = jwks;
        _cfg = cfg;
        _introspect = introspect;
        _cache = cache;
        _log = log;

        _mode = (cfg["Gateway:ValidationMode"] ?? "JwtFirst").Trim();
        _issuer = cfg["Gateway:Issuer"] ?? throw new InvalidOperationException("Gateway:Issuer missing");
        _audiences = cfg.GetSection("Gateway:ExpectedAudiences").Get<string[]>() ?? Array.Empty<string>();
        _clockSkew = TimeSpan.FromSeconds(cfg.GetValue("Gateway:TokenClockSkewSeconds", 60));

        _log.LogInformation("TokenAuthenticator initialized: Mode={Mode}, Issuer={Issuer}, Audiences={Audiences}",
            _mode, _issuer, string.Join(", ", _audiences));
    }

    public async Task<(bool ok, ClaimsPrincipal? principal, string source, string? error)>
        AuthenticateAsync(string bearerToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            _log.LogWarning("Empty bearer token provided");
            return (false, null, "none", "empty_token");
        }

        _log.LogDebug("=== TOKEN AUTHENTICATION START ===");
        _log.LogDebug("Validation Mode: {Mode}", _mode);

        return _mode.ToLowerInvariant() switch
        {
            "jwtonly" => await ValidateJwtOnlyAsync(bearerToken, ct),
            "jwtfirst" => await ValidateJwtFirstAsync(bearerToken, ct),
            _ => await ValidateJwtFirstAsync(bearerToken, ct)
        };
    }

    private async Task<(bool ok, ClaimsPrincipal? principal, string source, string? error)>
        ValidateJwtOnlyAsync(string bearerToken, CancellationToken ct)
    {
        _log.LogInformation("Attempting JWT-only validation");

        var (ok, principal, error) = await ValidateJwtInternalAsync(bearerToken, ct);

        if (ok && principal != null)
        {
            _log.LogInformation("✅ JWT validation SUCCEEDED");
            return (true, principal, "jwt", null);
        }

        _log.LogWarning("❌ JWT validation FAILED: {Error}", error);
        return (false, null, "jwt", error);
    }

    private async Task<(bool ok, ClaimsPrincipal? principal, string source, string? error)>
        ValidateJwtFirstAsync(string bearerToken, CancellationToken ct)
    {
        _log.LogInformation("Attempting JWT validation (JwtFirst mode)");

        var (jwtOk, jwtPrincipal, jwtError) = await ValidateJwtInternalAsync(bearerToken, ct);

        if (jwtOk && jwtPrincipal != null)
        {
            _log.LogInformation("✅ JWT validation SUCCEEDED");
            return (true, jwtPrincipal, "jwt", null);
        }

        _log.LogWarning("JWT validation failed: {Error}, falling back to introspection", jwtError);

        var (introOk, introPrincipal, introError) = await ValidateIntrospectionInternalAsync(bearerToken, ct);

        if (introOk && introPrincipal != null)
        {
            _log.LogInformation("✅ Introspection validation SUCCEEDED (fallback)");
            return (true, introPrincipal, "introspection", null);
        }

        _log.LogWarning("❌ Introspection validation also FAILED: {Error}", introError);
        return (false, null, "introspection", introError);
    }

    private async Task<(bool ok, ClaimsPrincipal? principal, string? error)>
        ValidateJwtInternalAsync(string bearerToken, CancellationToken ct)
    {
        JwtSecurityToken? jwt = null;

        try
        {
            jwt = new JwtSecurityTokenHandler().ReadJwtToken(bearerToken);
        }
        catch (Exception ex)
        {
            _log.LogDebug("Failed to parse JWT: {Error}", ex.Message);
            return (false, null, "not_a_jwt");
        }

        if (string.IsNullOrEmpty(jwt.Header.Kid))
        {
            _log.LogDebug("JWT has no KID header");
            return (false, null, "no_kid");
        }

        try
        {
            _log.LogDebug("Fetching JWKS for signature validation");
            var jwks = await _jwks.GetAsync(ct);
            var keys = jwks.GetSigningKeys();

            if (keys == null || !keys.Any())
            {
                _log.LogWarning("No signing keys found in JWKS");
                return (false, null, "no_signing_keys");
            }

            var tvp = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = _audiences.Length > 0,
                ValidAudiences = _audiences,
                ValidateLifetime = true,
                ClockSkew = _clockSkew
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(bearerToken, tvp, out _);

            return (true, principal, null);
        }
        catch (SecurityTokenSignatureKeyNotFoundException ex)
        {
            _log.LogWarning("Signature key not found, invalidating JWKS cache and retrying" + ex);

            await _jwks.InvalidateAsync(ct);

            try
            {
                var jwks = await _jwks.GetAsync(ct);
                var keys = jwks.GetSigningKeys();

                var tvp = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = keys,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = _audiences.Length > 0,
                    ValidAudiences = _audiences,
                    ValidateLifetime = true,
                    ClockSkew = _clockSkew
                };

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(bearerToken, tvp, out _);

                _log.LogInformation("JWT validation successful after JWKS refresh");
                return (true, principal, null);
            }
            catch (Exception retryEx)
            {
                _log.LogWarning("JWT validation failed after retry: {Error}", retryEx.Message);
                return (false, null, $"signature_validation_failed: {retryEx.Message}");
            }
        }
        catch (SecurityTokenExpiredException ex)
        {
            _log.LogDebug("JWT token expired" + ex);
            return (false, null, "token_expired");
        }
        catch (Exception ex)
        {
            _log.LogWarning("JWT validation failed: {Error}", ex.Message);
            return (false, null, $"jwt_validation_error: {ex.Message}");
        }
    }

    private async Task<(bool ok, ClaimsPrincipal? principal, string? error)>
        ValidateIntrospectionInternalAsync(string bearerToken, CancellationToken ct)
    {
        try
        {
            _log.LogDebug("Calling introspection endpoint");
            var (active, principal, error) = await _introspect.IntrospectAsync(bearerToken, ct);

            if (active && principal != null)
            {
                _log.LogDebug("Introspection returned active=true");
                return (true, principal, null);
            }

            _log.LogDebug("Introspection returned active=false or error: {Error}", error);
            return (false, null, error ?? "inactive");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Introspection call failed");
            return (false, null, $"introspection_error: {ex.Message}");
        }
    }
}