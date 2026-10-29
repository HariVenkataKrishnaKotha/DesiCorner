using DesiCorner.AuthServer.Identity; // ← Changed from Models
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.AuthServer.Data;

public static class DbInitializer
{
    public const string BootstrapAdminEmailKey = "BootstrapAdmin:Email";
    public const string BootstrapAdminPasswordKey = "BootstrapAdmin:Password";

    /// <summary>
    /// Seeds roles everywhere. Demo accounts with fixed passwords exist only in Development;
    /// elsewhere an admin is created only from BootstrapAdmin:Email and BootstrapAdmin:Password.
    /// </summary>
    public static async Task Initialize(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger logger)
    {
        // Ensure database is created (non-relational test providers have no migrations)
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        // Seed Roles
        var roles = new[]
        {
            new ApplicationRole { Name = "Admin" },
            new ApplicationRole { Name = "Customer" }
        };

        foreach (var role in roles)
        {
            if (role.Name != null && !await roleManager.RoleExistsAsync(role.Name))
            {
                await roleManager.CreateAsync(role);
            }
        }

        if (environment.IsDevelopment())
        {
            await SeedDemoAccountsAsync(userManager);
        }
        else
        {
            await SeedBootstrapAdminAsync(userManager, configuration, logger);
        }
    }

    private static async Task SeedBootstrapAdminAsync(
        UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
    {
        var email = configuration[BootstrapAdminEmailKey];
        var password = configuration[BootstrapAdminPasswordKey];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("{EmailKey} and {PasswordKey} are not both configured; no bootstrap admin is created",
                BootstrapAdminEmailKey, BootstrapAdminPasswordKey);
            return;
        }

        if (await userManager.FindByEmailAsync(email) != null)
        {
            logger.LogInformation("Bootstrap admin {Email} already exists; left unchanged", email);
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError("Bootstrap admin {Email} was not created: {Errors}",
                email, string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, "Admin");
        logger.LogInformation("Bootstrap admin {Email} created", email);
    }

    /// <summary>Development only: well-known accounts with fixed passwords.</summary>
    private static async Task SeedDemoAccountsAsync(UserManager<ApplicationUser> userManager)
    {
        // Seed Admin User
        var adminEmail = "admin@desicorner.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                RewardPoints = 0,
                DietaryPreference = "Veg",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Test User
        var testEmail = "test@desicorner.com";
        var testUser = await userManager.FindByEmailAsync(testEmail);

        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = testEmail,
                Email = testEmail,
                PhoneNumber = "1234567890",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                RewardPoints = 100,
                DietaryPreference = "Veg",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(testUser, "Test@123");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testUser, "Customer");
            }
        }
    }
}