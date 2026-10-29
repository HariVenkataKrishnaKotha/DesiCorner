using System.Security.Claims;
using DesiCorner.AuthServer.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using StackExchange.Redis;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// Hosts the real AuthServer (OpenIddict server, Identity, controllers) on the EF in-memory
/// provider. AuthServer's JwtBearer validation uses OpenIddict's own signing keys instead of
/// fetching JWKS over HTTP, so real tokens from /connect/token validate end to end.
/// Requests go to https://localhost so OpenIddict's HTTPS rule holds in every environment.
/// </summary>
public sealed class AuthServerFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://localhost:7001/";

    private readonly string _appDb = Guid.NewGuid().ToString();
    private readonly string _keysDb = Guid.NewGuid().ToString();
    private readonly string _environment;
    private readonly Dictionary<string, string?> _settings;

    public AuthServerFactory(string environment = "Development", IDictionary<string, string?>? settings = null)
    {
        _environment = environment;
        _settings = new Dictionary<string, string?>(settings ?? new Dictionary<string, string?>());
        ClientOptions.BaseAddress = new Uri("https://localhost/");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(_settings));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_appDb));
            services.RemoveAll<DbContextOptions<DataProtectionKeyContext>>();
            services.AddDbContext<DataProtectionKeyContext>(o => o.UseInMemoryDatabase(_keysDb));

            // Only the OTP path uses Redis; fail fast instead of waiting on a connection.
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false,connectTimeout=100,asyncTimeout=100,syncTimeout=100"));

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .PostConfigure<IOptionsMonitor<OpenIddictServerOptions>>((options, server) =>
                {
                    var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    foreach (var credentials in server.CurrentValue.SigningCredentials)
                    {
                        configuration.SigningKeys.Add(credentials.Key);
                    }
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
        });
    }

    /// <summary>
    /// A user access token shaped like the ones AuthServer issues (issuer, desicorner-api
    /// audience, sub, roles, scopes), signed with AuthServer's own signing key.
    /// </summary>
    public string CreateUserToken(Guid userId, string[] roles, params string[] scopes)
    {
        var server = Services.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;
        var claims = new List<Claim> { new("sub", userId.ToString()) };
        claims.AddRange(roles.Select(r => new Claim("role", r)));
        if (scopes.Length > 0)
        {
            claims.Add(new Claim("scope", string.Join(' ', scopes)));
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = "desicorner-api",
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = server.SigningCredentials.First()
        });
    }
}
