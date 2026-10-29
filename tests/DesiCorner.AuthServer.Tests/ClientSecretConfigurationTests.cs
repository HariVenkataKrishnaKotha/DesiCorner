using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// Confidential client secrets (OrderAPI's client-credentials client and the gateway's
/// introspection client) come only from configuration. Outside Development a missing secret
/// stops startup with a clear message; in Development the client is skipped with a warning.
/// </summary>
public class ClientSecretConfigurationTests
{
    private static async Task<object?> FindClient(AuthServerFactory factory, string clientId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>().FindByClientIdAsync(clientId);
    }

    [Theory]
    [InlineData(ServiceSecrets.OrderServiceKey)]
    [InlineData(ServiceSecrets.GatewayKey)]
    public void Outside_Development_a_missing_secret_stops_startup(string missingKey)
    {
        var settings = ServiceSecrets.Both(out _);
        settings.Remove(missingKey);
        using var factory = new AuthServerFactory("Production", settings);

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(missingKey, error.ToString());
    }

    [Fact]
    public async Task Outside_Development_configured_secrets_start_and_seed_both_clients()
    {
        using var factory = new AuthServerFactory("Production", ServiceSecrets.Both(out _));

        factory.CreateClient();

        Assert.NotNull(await FindClient(factory, ServiceSecrets.OrderServiceClientId));
        Assert.NotNull(await FindClient(factory, "desicorner-gateway"));
    }

    [Fact]
    public async Task In_Development_missing_secrets_skip_the_confidential_clients()
    {
        using var factory = new AuthServerFactory("Development");

        factory.CreateClient();

        Assert.Null(await FindClient(factory, ServiceSecrets.OrderServiceClientId));
        Assert.Null(await FindClient(factory, "desicorner-gateway"));
        Assert.NotNull(await FindClient(factory, "desicorner-angular"));
    }

    [Fact]
    public async Task The_gateway_client_secret_is_the_configured_one()
    {
        var settings = ServiceSecrets.Both(out _);
        using var factory = new AuthServerFactory("Development", settings);
        factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var apps = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var gateway = await apps.FindByClientIdAsync("desicorner-gateway");

        Assert.NotNull(gateway);
        Assert.True(await apps.ValidateClientSecretAsync(gateway!, settings[ServiceSecrets.GatewayKey]!));
    }
}
