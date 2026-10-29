using System.Net;
using System.Text;
using DesiCorner.Gateway.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace DesiCorner.Gateway.Tests;

/// <summary>
/// The introspection client secret comes only from configuration. Outside Development a
/// missing secret stops startup; in Development the gateway starts and introspection
/// reports itself unavailable instead of sending an empty secret to AuthServer.
/// </summary>
public class IntrospectionSecretTests
{
    private static async Task<(bool active, string? error)> Introspect(GatewayFactory factory)
    {
        factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IIntrospectionClient>();
        var (active, _, error) = await client.IntrospectAsync("opaque-token", CancellationToken.None);
        return (active, error);
    }

    [Fact]
    public void Outside_Development_a_missing_secret_stops_startup()
    {
        using var factory = new GatewayFactory { EnvironmentName = "Production", IntrospectionSecret = null };

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(GatewayFactory.IntrospectionSecretKey, error.ToString());
    }

    [Fact]
    public async Task Outside_Development_a_configured_secret_starts()
    {
        using var factory = new GatewayFactory { EnvironmentName = "Production", IntrospectionSecret = "configured-secret" };

        var response = await factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task In_Development_a_missing_secret_starts_without_calling_introspection()
    {
        using var factory = new GatewayFactory { EnvironmentName = "Development", IntrospectionSecret = null };

        var (active, error) = await Introspect(factory);

        Assert.False(active);
        Assert.Equal("introspection_unavailable", error);
        Assert.Empty(factory.Introspection.Requests);
    }

    [Fact]
    public async Task The_configured_secret_authenticates_the_introspection_request()
    {
        using var factory = new GatewayFactory { IntrospectionSecret = "configured-secret" };

        await Introspect(factory);

        var request = Assert.Single(factory.Introspection.Requests);
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("desicorner-gateway:configured-secret"));
        Assert.Equal($"Basic {expected}", request.Header("Authorization"));
    }
}
