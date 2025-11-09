using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RomaWatches.Models;

namespace RomaWatches.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer");

            try
            {
                // Ensure database is created and migrated
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrated successfully.");

                // Create admin user if not exists
                var adminEmail = "admin@example.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = "Admin",
                        LastName = "Account",
                        Role = "admin",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Admin user created successfully.");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError($"Failed to create admin user: {errors}");
                        throw new Exception($"Failed to create admin user: {errors}");
                    }
                }
                else
                {
                    // Update role and password if user exists
                    if (adminUser.Role != "admin")
                    {
                        adminUser.Role = "admin";
                        await userManager.UpdateAsync(adminUser);
                        logger.LogInformation("Admin user role updated.");
                    }
                    
                    // Reset password to ensure it's correct
                    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, "Admin@123");
                    if (resetResult.Succeeded)
                    {
                        logger.LogInformation("Admin user password reset successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }
}

