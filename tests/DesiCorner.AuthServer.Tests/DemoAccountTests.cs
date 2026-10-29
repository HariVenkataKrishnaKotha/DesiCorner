using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// The demo accounts (admin@desicorner.com and test@desicorner.com with fixed passwords) are
/// created only in Development. Elsewhere no account with a hard-coded password is created;
/// an admin is bootstrapped only when BootstrapAdmin:Email and BootstrapAdmin:Password are both
/// configured. Roles, clients and scopes are seeded in every environment.
/// </summary>
public class DemoAccountTests
{
    private const string BootstrapEmailKey = "BootstrapAdmin:Email";
    private const string BootstrapPasswordKey = "BootstrapAdmin:Password";
    private static readonly string[] DemoEmails = { "admin@desicorner.com", "test@desicorner.com" };

    private static async Task<ApplicationUser?> FindUser(AuthServerFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
    }

    private static AuthServerFactory Production(Action<Dictionary<string, string?>>? configure = null)
    {
        var settings = ServiceSecrets.Both(out _);
        configure?.Invoke(settings);
        var factory = new AuthServerFactory("Production", settings);
        factory.CreateClient();
        return factory;
    }

    [Fact]
    public async Task Development_seeds_the_demo_accounts()
    {
        using var factory = new AuthServerFactory();
        factory.CreateClient();

        foreach (var email in DemoEmails)
        {
            Assert.NotNull(await FindUser(factory, email));
        }
    }

    [Fact]
    public async Task Outside_Development_no_demo_account_is_created_but_roles_are()
    {
        using var factory = Production();

        foreach (var email in DemoEmails)
        {
            Assert.Null(await FindUser(factory, email));
        }

        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        Assert.True(await roles.RoleExistsAsync("Admin"));
        Assert.True(await roles.RoleExistsAsync("Customer"));
    }

    [Fact]
    public async Task Outside_Development_a_configured_bootstrap_admin_is_created()
    {
        var email = "owner@example.test";
        var password = $"Ab1!{ServiceSecrets.New()}";
        using var factory = Production(s =>
        {
            s[BootstrapEmailKey] = email;
            s[BootstrapPasswordKey] = password;
        });

        var admin = await FindUser(factory, email);

        Assert.NotNull(admin);
        Assert.True(admin.EmailConfirmed);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await users.IsInRoleAsync(admin, "Admin"));
        Assert.True(await users.CheckPasswordAsync(admin, password));
        foreach (var demo in DemoEmails)
        {
            Assert.Null(await FindUser(factory, demo));
        }
    }

    [Fact]
    public async Task Outside_Development_a_bootstrap_email_without_a_password_creates_no_account()
    {
        using var factory = Production(s => s[BootstrapEmailKey] = "owner@example.test");

        Assert.Null(await FindUser(factory, "owner@example.test"));
        foreach (var demo in DemoEmails)
        {
            Assert.Null(await FindUser(factory, demo));
        }
    }
}
