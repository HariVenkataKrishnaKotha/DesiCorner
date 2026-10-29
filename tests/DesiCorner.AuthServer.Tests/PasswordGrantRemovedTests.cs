using System.Net;
using System.Text.Json;
using DesiCorner.AuthServer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// The resource-owner password grant is gone: the SPA signs in with authorization code + PKCE,
/// and nothing else in the repository posts grant_type=password.
/// </summary>
public class PasswordGrantRemovedTests
{
    [Fact]
    public async Task A_password_grant_token_request_is_rejected()
    {
        using var factory = new AuthServerFactory();

        var response = await factory.CreateClient().PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "desicorner-angular",
            ["username"] = "test@desicorner.com",
            ["password"] = "Test@123",
            ["scope"] = "openid desicorner.cart"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(Errors.UnsupportedGrantType, body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Discovery_does_not_advertise_the_password_grant()
    {
        using var factory = new AuthServerFactory();

        using var discovery = JsonDocument.Parse(await factory.CreateClient().GetStringAsync("/.well-known/openid-configuration"));

        var grants = discovery.RootElement.GetProperty("grant_types_supported").EnumerateArray().Select(g => g.GetString());
        Assert.DoesNotContain(GrantTypes.Password, grants);
    }

    [Fact]
    public async Task Seeding_removes_the_password_permission_from_an_existing_SPA_client()
    {
        using var factory = new AuthServerFactory();
        factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            // As seeded by earlier builds
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var spa = (await manager.FindByClientIdAsync("desicorner-angular"))!;
            var descriptor = new OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, spa);
            descriptor.Permissions.Add(Permissions.GrantTypes.Password);
            await manager.UpdateAsync(spa, descriptor);
        }

        await Seed.InitializeAsync(factory.Services, AuthServerFactory.Issuer);

        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var spa = (await manager.FindByClientIdAsync("desicorner-angular"))!;
            Assert.False(await manager.HasPermissionAsync(spa, Permissions.GrantTypes.Password));
            Assert.True(await manager.HasPermissionAsync(spa, Permissions.GrantTypes.AuthorizationCode));
        }
    }
}
