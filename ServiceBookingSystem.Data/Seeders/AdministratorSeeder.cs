using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Core.Contracts;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Data.Seeders;

/// <summary>
/// A seeder responsible for creating the initial administrator user.
/// This ensures that there is always at least one superuser account in the system.
/// </summary>
public class AdministratorSeeder : ISeeder
{
    /// <summary>
    /// Executes the seeding logic for the administrator user.
    /// </summary>
    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<AdministratorSeeder>>();

        // Check if any user already has the Administrator role.
        var adminUsers = await userManager.GetUsersInRoleAsync(RoleConstants.Administrator);
        if (adminUsers.Any())
        {
            logger.LogInformation("An administrator user already exists. Seeding is not required.");
            return;
        }

        // If no admin user exists, create a new one.
        var admin = new ApplicationUser
        {
            UserName = "admin@servicebooking.com",
            Email = "admin@servicebooking.com",
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };
        
        // Create the user with a default password.
        // In a real production app, this password should come from a secure configuration source.
        var result = await userManager.CreateAsync(admin, "admin123");

        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create administrator user. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // If the user was created successfully, assign them to the Administrator role.
        await userManager.AddToRoleAsync(admin, RoleConstants.Administrator);
        logger.LogInformation("Administrator user 'admin@servicebooking.com' created successfully and assigned to the '{AdminRole}' role.", RoleConstants.Administrator);
    }
}
