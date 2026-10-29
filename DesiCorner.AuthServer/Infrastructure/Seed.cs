using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DesiCorner.AuthServer.Infrastructure;

/// <summary>
/// Seeds roles, OpenIddict clients and scopes. Clients and scopes are reconciled: missing ones
/// are created, existing ones are updated when the fields managed here (permissions, redirect
/// URIs, requirements, secret, scope resources and texts) differ, and otherwise left untouched.
/// </summary>
public static class Seed
{
    public static async Task InitializeAsync(IServiceProvider services, string issuer)
    {
        using var scope = services.CreateScope();

        await SeedRolesAsync(scope.ServiceProvider);
        await SeedClientsAsync(scope.ServiceProvider);
        await SeedServiceClientsAsync(scope.ServiceProvider);
        await SeedScopesAsync(scope.ServiceProvider);
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        string[] roles = { "Customer", "Admin" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }
    }

    private static async Task SeedClientsAsync(IServiceProvider services)
    {
        // Angular SPA Client (Authorization Code + PKCE)
        await ReconcileClientAsync(services, new OpenIddictApplicationDescriptor
        {
            ClientId = "desicorner-angular",
            ClientType = ClientTypes.Public, // No client secret for SPA
            DisplayName = "DesiCorner Angular Application",

            RedirectUris =
            {
                new Uri("http://localhost:4200/auth/callback"),
                new Uri("https://localhost:4200/auth/callback")
            },

            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:4200"),
                new Uri("https://localhost:4200")
            },

            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Revocation,
                Permissions.Endpoints.EndSession,

                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.GrantTypes.Password,

                Permissions.ResponseTypes.Code,

                // Scopes
                Permissions.Prefixes.Scope + Scopes.OpenId,
                Permissions.Prefixes.Scope + Scopes.Email,
                Permissions.Prefixes.Scope + Scopes.Profile,
                Permissions.Prefixes.Scope + Scopes.Phone,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess,
                Permissions.Prefixes.Scope + "desicorner.products.read",
                Permissions.Prefixes.Scope + "desicorner.products.write",
                Permissions.Prefixes.Scope + "desicorner.cart",
                Permissions.Prefixes.Scope + "desicorner.orders.read",
                Permissions.Prefixes.Scope + "desicorner.orders.write",
                Permissions.Prefixes.Scope + "desicorner.payment",
                Permissions.Prefixes.Scope + "desicorner.admin"
            },

            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        });

        // YARP Gateway Client (for introspection). The secret comes only from configuration.
        var gatewaySecret = services.GetRequiredService<IConfiguration>()[ClientSecrets.GatewayKey];
        if (string.IsNullOrWhiteSpace(gatewaySecret))
        {
            Logger(services).LogWarning(
                "{Key} is not configured; the desicorner-gateway client is not seeded and token introspection is unavailable",
                ClientSecrets.GatewayKey);
            return;
        }

        await ReconcileClientAsync(services, new OpenIddictApplicationDescriptor
        {
            ClientId = "desicorner-gateway",
            ClientSecret = gatewaySecret,
            ClientType = ClientTypes.Confidential,
            DisplayName = "DesiCorner API Gateway",
            Permissions =
            {
                Permissions.Endpoints.Introspection,
                Permissions.Endpoints.Revocation
            }
        });
    }

    private static async Task SeedServiceClientsAsync(IServiceProvider services)
    {
        // OrderAPI (confidential): client-credentials only, for the user-lookup scope
        var secret = services.GetRequiredService<IConfiguration>()[ClientSecrets.OrderServiceKey];
        if (string.IsNullOrWhiteSpace(secret))
        {
            Logger(services).LogWarning(
                "{Key} is not configured; the {ClientId} client is not seeded and guest-order account linking is unavailable",
                ClientSecrets.OrderServiceKey, InternalApi.OrderServiceClientId);
            return;
        }

        await ReconcileClientAsync(services, new OpenIddictApplicationDescriptor
        {
            ClientId = InternalApi.OrderServiceClientId,
            ClientSecret = secret,
            ClientType = ClientTypes.Confidential,
            DisplayName = "DesiCorner Order Service",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + InternalApi.UserLookupScope
            }
        });
    }

    private static async Task SeedScopesAsync(IServiceProvider services)
    {
        var manager = services.GetRequiredService<IOpenIddictScopeManager>();

        var scopes = new[]
        {
            new { Name = "desicorner.products.read", DisplayName = "Read products", Description = "View products and categories" },
            new { Name = "desicorner.products.write", DisplayName = "Write products", Description = "Manage products and categories (Admin only)" },
            new { Name = "desicorner.cart", DisplayName = "Shopping cart", Description = "Manage shopping cart" },
            new { Name = "desicorner.orders.read", DisplayName = "Read orders", Description = "View order history" },
            new { Name = "desicorner.orders.write", DisplayName = "Write orders", Description = "Create and manage orders" },
            new { Name = "desicorner.payment", DisplayName = "Payment", Description = "Process payments" },
            new { Name = "desicorner.admin", DisplayName = "Admin access", Description = "Full administrative access" },
            new { Name = InternalApi.UserLookupScope, DisplayName = "User lookup (service)", Description = "Service-to-service: find an account by email or phone" }
        };

        foreach (var scope in scopes)
        {
            var desired = new OpenIddictScopeDescriptor
            {
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Resources = { "desicorner-api" }
            };

            var existing = await manager.FindByNameAsync(scope.Name);
            if (existing is null)
            {
                await manager.CreateAsync(desired);
                continue;
            }

            var current = new OpenIddictScopeDescriptor();
            await manager.PopulateAsync(current, existing);
            if (current.DisplayName == desired.DisplayName &&
                current.Description == desired.Description &&
                current.Resources.SetEquals(desired.Resources))
            {
                continue;
            }

            current.DisplayName = desired.DisplayName;
            current.Description = desired.Description;
            Replace(current.Resources, desired.Resources);
            await manager.UpdateAsync(existing, current);
            Logger(services).LogInformation("Updated OpenIddict scope {Scope}", scope.Name);
        }
    }

    /// <summary>
    /// Creates the client, or updates the existing one when a managed field or the secret differs.
    /// The descriptor populated from the stored client carries the stored (hashed) secret, which
    /// UpdateAsync leaves as is; a plaintext secret that differs from it is hashed by UpdateAsync.
    /// </summary>
    private static async Task ReconcileClientAsync(IServiceProvider services, OpenIddictApplicationDescriptor desired)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();

        var existing = await manager.FindByClientIdAsync(desired.ClientId!);
        if (existing is null)
        {
            await manager.CreateAsync(desired);
            return;
        }

        var current = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(current, existing);

        var secretChanged = desired.ClientSecret is { } secret &&
                            !await manager.ValidateClientSecretAsync(existing, secret);

        if (!secretChanged &&
            current.ClientType == desired.ClientType &&
            current.DisplayName == desired.DisplayName &&
            current.Permissions.SetEquals(desired.Permissions) &&
            current.RedirectUris.SetEquals(desired.RedirectUris) &&
            current.PostLogoutRedirectUris.SetEquals(desired.PostLogoutRedirectUris) &&
            current.Requirements.SetEquals(desired.Requirements))
        {
            return;
        }

        current.ClientType = desired.ClientType;
        current.DisplayName = desired.DisplayName;
        Replace(current.Permissions, desired.Permissions);
        Replace(current.RedirectUris, desired.RedirectUris);
        Replace(current.PostLogoutRedirectUris, desired.PostLogoutRedirectUris);
        Replace(current.Requirements, desired.Requirements);
        if (secretChanged)
        {
            current.ClientSecret = desired.ClientSecret;
        }

        await manager.UpdateAsync(existing, current);
        Logger(services).LogInformation("Updated OpenIddict client {ClientId}{Secret}",
            desired.ClientId, secretChanged ? " (secret rotated)" : "");
    }

    private static void Replace<T>(ISet<T> target, IEnumerable<T> source)
    {
        target.Clear();
        target.UnionWith(source);
    }

    private static ILogger Logger(IServiceProvider services) =>
        services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Seed));
}
