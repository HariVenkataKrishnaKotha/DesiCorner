using DesiCorner.AuthServer.Data;
using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DesiCorner.AuthServer.Infrastructure;

public static class Seed
{
    public static async Task InitializeAsync(IServiceProvider services, string issuer)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(scope.ServiceProvider);
        await SeedClientsAsync(scope.ServiceProvider, issuer);
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

    private static async Task SeedClientsAsync(IServiceProvider services, string issuer)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();

        // Angular SPA Client (Authorization Code + PKCE)
        var angularClient = await manager.FindByClientIdAsync("desicorner-angular");
        if (angularClient is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
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
        }
        else
        {
            var descriptor = new OpenIddictApplicationDescriptor();

            await manager.PopulateAsync(descriptor, angularClient);

            // Update existing client to support password grant
            descriptor.ClientType = ClientTypes.Confidential;
            descriptor.ClientSecret = "secret_for_testing_password_grant"; 

            // Ensure Password grant permission exists
            if (!descriptor.Permissions.Contains(Permissions.GrantTypes.Password))
            {
                descriptor.Permissions.Add(Permissions.GrantTypes.Password);
            }

            // Update the client with modified descriptor
            await manager.UpdateAsync(angularClient, descriptor);
        }

        // YARP Gateway Client (for introspection)
        var gatewayClient = await manager.FindByClientIdAsync("desicorner-gateway");
        if (gatewayClient is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "desicorner-gateway",
                ClientSecret = "desicorner-gateway-secret-change-in-production",
                ClientType = ClientTypes.Confidential,
                DisplayName = "DesiCorner API Gateway",
                Permissions =
                {
                    Permissions.Endpoints.Introspection,
                    Permissions.Endpoints.Revocation
                }
            });
        }
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
            new { Name = "desicorner.admin", DisplayName = "Admin access", Description = "Full administrative access" }
        };

        foreach (var scope in scopes)
        {
            var existingScope = await manager.FindByNameAsync(scope.Name);
            if (existingScope is null)
            {
                await manager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = scope.Name,
                    DisplayName = scope.DisplayName,
                    Description = scope.Description,
                    Resources = { "desicorner-api" }
                });
            }
        }
    }
}