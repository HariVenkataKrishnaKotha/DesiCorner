using DesiCorner.AuthServer.Data;
using DesiCorner.AuthServer.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DesiCorner.AuthServer.Tests;

/// <summary>
/// Outside Development, an account that already has the bootstrap email is left unchanged,
/// unless BootstrapAdmin:ResetPassword is true: then its password is reset to the configured one
/// and it is made an Admin. The flag is meant to be set for one restart and removed again.
/// </summary>
public class BootstrapAdminResetTests
{
    private const string Email = "owner@example.test";
    private const string EmailKey = "BootstrapAdmin:Email";
    private const string PasswordKey = "BootstrapAdmin:Password";
    private const string ResetKey = "BootstrapAdmin:ResetPassword";

    private static string NewPassword() => $"Ab1!{ServiceSecrets.New()}";

    private static AuthServerFactory Production()
    {
        var factory = new AuthServerFactory("Production", ServiceSecrets.Both(out _));
        factory.CreateClient();
        return factory;
    }

    /// <summary>Runs the startup seeding again, as the next restart would.</summary>
    private static async Task Restart(AuthServerFactory factory, string password, bool reset = false)
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        configuration[EmailKey] = Email;
        configuration[PasswordKey] = password;
        configuration[ResetKey] = reset ? "true" : null;

        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        await DbInitializer.Initialize(
            services.GetRequiredService<ApplicationDbContext>(),
            services.GetRequiredService<UserManager<ApplicationUser>>(),
            services.GetRequiredService<RoleManager<ApplicationRole>>(),
            services.GetRequiredService<IHostEnvironment>(),
            configuration,
            services.GetRequiredService<ILoggerFactory>().CreateLogger("test"));
    }

    private static async Task<ApplicationUser> CreateCustomer(AuthServerFactory factory, string password)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = Email, Email = Email, CreatedAt = DateTime.UtcNow };
        Assert.True((await users.CreateAsync(user, password)).Succeeded);
        await users.AddToRoleAsync(user, "Customer");
        return user;
    }

    private static async Task<(bool passwordValid, bool isAdmin, ApplicationUser user)> Inspect(AuthServerFactory factory, string password)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByEmailAsync(Email))!;
        return (await users.CheckPasswordAsync(user, password), await users.IsInRoleAsync(user, "Admin"), user);
    }

    [Fact]
    public async Task Without_the_flag_an_existing_admin_keeps_its_password()
    {
        using var factory = Production();
        var original = NewPassword();
        await Restart(factory, original);

        await Restart(factory, NewPassword());

        Assert.True((await Inspect(factory, original)).passwordValid);
    }

    [Fact]
    public async Task Without_the_flag_an_existing_customer_is_not_promoted()
    {
        using var factory = Production();
        var own = NewPassword();
        await CreateCustomer(factory, own);

        await Restart(factory, NewPassword());

        var (passwordValid, isAdmin, _) = await Inspect(factory, own);
        Assert.True(passwordValid);
        Assert.False(isAdmin);
    }

    [Fact]
    public async Task With_the_flag_the_password_and_admin_role_are_reset()
    {
        using var factory = Production();
        var old = NewPassword();
        await CreateCustomer(factory, old);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var customer = (await users.FindByEmailAsync(Email))!;
            await users.SetLockoutEndDateAsync(customer, DateTimeOffset.UtcNow.AddHours(1));
        }
        var replacement = NewPassword();

        await Restart(factory, replacement, reset: true);

        var (passwordValid, isAdmin, user) = await Inspect(factory, replacement);
        Assert.True(passwordValid);
        Assert.False((await Inspect(factory, old)).passwordValid);
        Assert.True(isAdmin);
        Assert.True(user.EmailConfirmed);
        Assert.True(user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task With_the_flag_a_password_that_fails_the_policy_changes_nothing()
    {
        using var factory = Production();
        var old = NewPassword();
        await CreateCustomer(factory, old);

        await Restart(factory, "weak", reset: true);

        var (passwordValid, isAdmin, _) = await Inspect(factory, old);
        Assert.True(passwordValid);
        Assert.False(isAdmin);
    }
}
