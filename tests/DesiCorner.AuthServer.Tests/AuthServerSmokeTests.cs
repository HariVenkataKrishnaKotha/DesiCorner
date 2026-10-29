using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace DesiCorner.AuthServer.Tests;

public class AuthServerSmokeTests
{
    [Fact]
    public async Task Starts_on_the_in_memory_provider_and_seeds_the_SPA_client()
    {
        using var factory = new AuthServerFactory();

        var health = await factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        using var scope = factory.Services.CreateScope();
        var apps = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        Assert.NotNull(await apps.FindByClientIdAsync("desicorner-angular"));
    }

    [Fact]
    public async Task Serves_the_discovery_document()
    {
        using var factory = new AuthServerFactory();

        var response = await factory.CreateClient().GetAsync("/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"token_endpoint\"", await response.Content.ReadAsStringAsync());
    }
}
