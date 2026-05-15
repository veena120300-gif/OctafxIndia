using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OctafxIndia.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OctafxIndia.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            // Ensure the Admin role exists
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!roleResult.Succeeded)
                {
                    logger.LogWarning("Could not create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            // Seed a predefined list of users
            var usersToSeed = new[]
            {
                new { Email = "user1@example.com", Password = "UserPass123!" },
                new { Email = "user2@example.com", Password = "UserPass123!" },
                new { Email = "user3@example.com", Password = "UserPass123!" },
                new { Email = "user4@example.com", Password = "UserPass123!" }
            };

            foreach (var userData in usersToSeed)
            {
                if (await userManager.FindByEmailAsync(userData.Email) == null)
                {
                    var user = new ApplicationUser { UserName = userData.Email, Email = userData.Email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, userData.Password);
                    if (!result.Succeeded)
                    {
                        logger.LogWarning("Failed to create user {Email}: {Errors}", userData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // Ensure the admin user exists and is assigned the Admin role
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "AdminPass123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to create admin user {Email}: {Errors}", adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                var result = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to assign Admin role to user {Email}: {Errors}", adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Seed additional site data if the table is empty
            try
            {
                if (dbContext.SiteData != null && !await dbContext.SiteData.AnyAsync())
                {
                    dbContext.SiteData.AddRange(
                        new SiteData { Key = "HeroTitle", Value = "Welcome - edit via admin" },
                        new SiteData { Key = "Footer", Value = "© OctafxIndia" }
                    );
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SiteData seeding was skipped or failed. This may be expected if the table does not exist.");
            }
        }
    }
}
