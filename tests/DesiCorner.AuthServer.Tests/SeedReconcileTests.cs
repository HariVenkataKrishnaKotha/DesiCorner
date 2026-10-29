using DesiCorner.AuthServer.Data;
using DesiCorner.AuthServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// Seeding used to create clients and scopes only when missing, so a database seeded by an
/// older build kept stale permissions, redirect URIs and secrets. It now brings existing
/// entries in line with the code and configuration, and a run with nothing to change writes
/// nothing.
/// </summary>
public class SeedReconcileTests
{
    private static Task Reseed(AuthServerFactory factory) =>
        Seed.InitializeAsync(factory.Services, AuthServerFactory.Issuer);

    private static AuthServerFactory Started(out string orderServiceSecret, out string gatewaySecret)
    {
        var settings = ServiceSecrets.Both(out orderServiceSecret);
        gatewaySecret = settings[ServiceSecrets.GatewayKey]!;
        var factory = new AuthServerFactory(settings: settings);
        factory.CreateClient();
        return factory;
    }

    private static Dictionary<string, string?> ConcurrencyTokens(AuthServerFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return db.Set<OpenIddictEntityFrameworkCoreApplication>().AsNoTracking().AsEnumerable()
            .Select(app => ("client:" + app.ClientId, app.ConcurrencyToken))
            .Concat(db.Set<OpenIddictEntityFrameworkCoreScope>().AsNoTracking().AsEnumerable()
                .Select(s => ("scope:" + s.Name, s.ConcurrencyToken)))
            .ToDictionary(e => e.Item1, e => e.Item2);
    }

    /// <summary>Rewrites a stored client the way an older build might have left it.</summary>
    private static async Task Tamper(AuthServerFactory factory, string clientId, Action<OpenIddictApplicationDescriptor> change)
    {
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var app = await manager.FindByClientIdAsync(clientId) ?? throw new InvalidOperationException(clientId);
        var descriptor = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(descriptor, app);
        change(descriptor);
        await manager.UpdateAsync(app, descriptor);
    }

    private static async Task<OpenIddictApplicationDescriptor> Client(AuthServerFactory factory, string clientId)
    {
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var descriptor = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(descriptor, (await manager.FindByClientIdAsync(clientId))!);
        return descriptor;
    }

    private static async Task<bool> SecretValidates(AuthServerFactory factory, string clientId, string secret)
    {
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        return await manager.ValidateClientSecretAsync((await manager.FindByClientIdAsync(clientId))!, secret);
    }

    [Fact]
    public async Task A_second_run_changes_nothing()
    {
        using var factory = Started(out _, out _);
        var before = ConcurrencyTokens(factory);

        await Reseed(factory);

        Assert.Equal(3, before.Keys.Count(k => k.StartsWith("client:")));
        Assert.Equal(before, ConcurrencyTokens(factory));
    }

    [Fact]
    public async Task Existing_clients_get_the_permissions_and_redirect_uris_from_code()
    {
        using var factory = Started(out _, out _);
        await Tamper(factory, "desicorner-angular", d =>
        {
            d.Permissions.Remove(Permissions.Prefixes.Scope + "desicorner.cart");
            d.Permissions.Add(Permissions.GrantTypes.Implicit);
            d.RedirectUris.Add(new Uri("https://stale.example.test/auth/callback"));
            d.PostLogoutRedirectUris.Clear();
        });
        await Tamper(factory, ServiceSecrets.OrderServiceClientId, d =>
            d.Permissions.Remove(Permissions.Prefixes.Scope + ServiceSecrets.UserLookupScope));

        await Reseed(factory);

        var angular = await Client(factory, "desicorner-angular");
        Assert.Contains(Permissions.Prefixes.Scope + "desicorner.cart", angular.Permissions);
        Assert.DoesNotContain(Permissions.GrantTypes.Implicit, angular.Permissions);
        Assert.Equal(
            new[] { "http://localhost:4200/auth/callback", "https://localhost:4200/auth/callback" },
            angular.RedirectUris.Select(u => u.AbsoluteUri).Order());
        Assert.Equal(
            new[] { "http://localhost:4200/", "https://localhost:4200/" },
            angular.PostLogoutRedirectUris.Select(u => u.AbsoluteUri).Order());
        Assert.Contains(Requirements.Features.ProofKeyForCodeExchange, angular.Requirements);

        var orderService = await Client(factory, ServiceSecrets.OrderServiceClientId);
        Assert.Contains(Permissions.Prefixes.Scope + ServiceSecrets.UserLookupScope, orderService.Permissions);
    }

    [Fact]
    public async Task Reconciling_permissions_keeps_the_stored_secret_valid()
    {
        using var factory = Started(out var orderServiceSecret, out _);
        await Tamper(factory, ServiceSecrets.OrderServiceClientId, d => d.Permissions.Remove(Permissions.Endpoints.Token));

        await Reseed(factory);

        Assert.True(await SecretValidates(factory, ServiceSecrets.OrderServiceClientId, orderServiceSecret));
    }

    [Fact]
    public async Task A_rotated_secret_in_configuration_replaces_the_stored_one()
    {
        using var factory = Started(out var oldOrderServiceSecret, out var oldGatewaySecret);
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var newOrderServiceSecret = ServiceSecrets.New();
        var newGatewaySecret = ServiceSecrets.New();
        configuration[ServiceSecrets.OrderServiceKey] = newOrderServiceSecret;
        configuration[ServiceSecrets.GatewayKey] = newGatewaySecret;

        await Reseed(factory);

        Assert.True(await SecretValidates(factory, ServiceSecrets.OrderServiceClientId, newOrderServiceSecret));
        Assert.False(await SecretValidates(factory, ServiceSecrets.OrderServiceClientId, oldOrderServiceSecret));
        Assert.True(await SecretValidates(factory, "desicorner-gateway", newGatewaySecret));
        Assert.False(await SecretValidates(factory, "desicorner-gateway", oldGatewaySecret));
    }

    [Fact]
    public async Task Existing_scopes_get_the_resources_and_texts_from_code()
    {
        using var factory = Started(out _, out _);
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
            var cart = (await manager.FindByNameAsync("desicorner.cart"))!;
            var descriptor = new OpenIddictScopeDescriptor();
            await manager.PopulateAsync(descriptor, cart);
            descriptor.Resources.Clear();
            descriptor.Description = "stale";
            await manager.UpdateAsync(cart, descriptor);
        }

        await Reseed(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
            var cart = (await manager.FindByNameAsync("desicorner.cart"))!;
            Assert.Equal(new[] { "desicorner-api" }, await manager.GetResourcesAsync(cart));
            Assert.Equal("Manage shopping cart", await manager.GetDescriptionAsync(cart));
        }
    }
}
