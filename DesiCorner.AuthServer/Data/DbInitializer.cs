using DesiCorner.AuthServer.Identity; // ← Changed from Models
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.AuthServer.Data;

public static class DbInitializer
{
    public static async Task Initialize(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

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