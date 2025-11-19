using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Core.Contracts;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Data.Seeders;

/// <summary>
/// A seeder responsible for ensuring that the fundamental user roles
/// (Administrator, Provider, Customer) exist in the database.
/// </summary>
public class RolesSeeder : ISeeder
{
    /// <summary>
    /// Executes the seeding logic for roles.
    /// </summary>
    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<RolesSeeder>>();

        var roleNames = new[]
        {
            RoleConstants.Administrator,
            RoleConstants.Provider,
            RoleConstants.Customer
        };

        foreach (var roleName in roleNames)
        {
            await SeedRoleAsync(roleManager, logger, roleName);
        }
    }

    /// <summary>
    /// A helper method to create a single role if it does not already exist.
    /// </summary>
    private static async Task SeedRoleAsync(RoleManager<ApplicationRole> roleManager, ILogger logger, string roleName)
    {
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (roleExists)
        {
            logger.LogInformation("Role '{RoleName}' already exists. Seeding is not required.", roleName);
            return;
        }

        var role = new ApplicationRole(roleName);
        var result = await roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            // If role creation fails, throw an exception to halt the startup process.
            // The errors are concatenated to provide a clear message.
            throw new Exception($"Failed to create role '{roleName}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        
        logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
    }
}
