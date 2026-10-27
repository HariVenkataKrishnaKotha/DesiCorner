using System.Security.Claims;
using DesiCorner.Gateway.Auth;
using DesiCorner.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ReverseProxy.Forwarder;

namespace DesiCorner.Gateway.Tests;

/// <summary>
/// Hosts the real gateway pipeline (middleware + YARP) but records what it would send
/// to the downstream services instead of opening connections to them.
/// </summary>
public sealed class GatewayFactory : WebApplicationFactory<Program>
{
    public RecordingHandler Upstream { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRedisRateLimiter>();
            services.AddSingleton<IRedisRateLimiter, NoRateLimiter>();

            services.RemoveAll<ITokenAuthenticator>();
            services.AddSingleton<ITokenAuthenticator, FakeTokenAuthenticator>();

            services.RemoveAll<IForwarderHttpClientFactory>();
            services.AddSingleton<IForwarderHttpClientFactory>(new RecordingForwarderFactory(Upstream));
        });
    }

    private sealed class NoRateLimiter : IRedisRateLimiter
    {
        public Task<bool> ShouldLimitAsync(string bucketKey, int maxHits, TimeSpan window, CancellationToken ct) =>
            Task.FromResult(false);
    }

    private sealed class RecordingForwarderFactory(RecordingHandler handler) : IForwarderHttpClientFactory
    {
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) =>
            new(handler, disposeHandler: false);
    }
}

/// <summary>
/// Accepts "valid-{guid}" bearer tokens as a user with that subject; rejects everything else.
/// </summary>
public sealed class FakeTokenAuthenticator : ITokenAuthenticator
{
    public const string Prefix = "valid-";

    public static string TokenFor(Guid userId) => Prefix + userId;

    public Task<(bool ok, ClaimsPrincipal? principal, string source, string? error)> AuthenticateAsync(
        string bearerToken, CancellationToken ct)
    {
        if (bearerToken.StartsWith(Prefix) && Guid.TryParse(bearerToken[Prefix.Length..], out var userId))
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("email", $"{userId}@example.test"),
                new Claim(ClaimTypes.Role, "Customer")
            }, authenticationType: "jwt");
            return Task.FromResult<(bool, ClaimsPrincipal?, string, string?)>((true, new ClaimsPrincipal(identity), "jwt", null));
        }

        return Task.FromResult<(bool, ClaimsPrincipal?, string, string?)>((false, null, "jwt", "invalid_token"));
    }
}
