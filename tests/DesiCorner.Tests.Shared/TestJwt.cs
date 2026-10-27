using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DesiCorner.Tests.Shared;

/// <summary>
/// Issues JWTs signed with a per-process random key and points a service's JwtBearer
/// handler at that key, so tests exercise real token validation without the AuthServer.
/// </summary>
public static class TestJwt
{
    public const string Issuer = "https://auth.test.local/";
    public const string Audience = "desicorner-api";

    private static readonly SymmetricSecurityKey SigningKey =
        new(RandomNumberGenerator.GetBytes(32)) { KeyId = "test-key" };

    public static string CreateToken(Guid userId, string? email = null, string? phone = null, params string[] roles)
    {
        var claims = new List<Claim> { new("sub", userId.ToString()) };
        if (email != null) claims.Add(new Claim("email", email));
        if (phone != null) claims.Add(new Claim("phone_number", phone));
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Replaces the OIDC discovery-based configuration with the test signing key.
    /// Runs after the app's own JwtBearer setup, so everything else (claim mapping,
    /// lifetime checks, role/name claim types) stays as the app configured it.
    /// </summary>
    public static IServiceCollection UseTestJwtBearer(this IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            o.Authority = null;
            o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                new OpenIdConnectConfiguration { Issuer = Issuer });
            o.TokenValidationParameters.IssuerSigningKey = SigningKey;
            o.TokenValidationParameters.ValidIssuer = Issuer;
            o.TokenValidationParameters.ValidAudiences = new[] { Audience };
        });
        return services;
    }
}
