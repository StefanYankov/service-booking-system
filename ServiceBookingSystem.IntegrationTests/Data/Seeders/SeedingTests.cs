using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Data.Seeders;

public class SeedingTests : BaseIntegrationTest
{
    public SeedingTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Seeders_OnFirstRun_ShouldPopulateRolesAndAdminUser()
    {
        // Arrange:
        var roleManager = this.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        
        // Act & Assert:
        var adminRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Administrator);
        var providerRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Provider);
        var customerRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Customer);

        Assert.True(adminRoleExists, "the Administrator role is essential for system management.");
        Assert.True(providerRoleExists, "the Provider role is essential for offering services.");
        Assert.True(customerRoleExists, "the Customer role is essential for booking services.");  

        var adminSeeder = this.ServiceProvider.GetRequiredService<AdministratorSeeder>();
        await adminSeeder.SeedAsync(this.ServiceProvider);
        
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync("admin@servicebooking.com");
        Assert.NotNull(adminUser); // "a default administrator user should be created."
        
        var isAdmin = await userManager.IsInRoleAsync(adminUser, RoleConstants.Administrator);
        Assert.True(isAdmin, "the default user must be assigned the Administrator role.");
    }
    
    [Fact]
    public async Task Seeders_WhenRunMultipleTimes_ShouldBeIdempotent()
    {
        // Arrange:
        var rolesSeeder = this.ServiceProvider.GetRequiredService<RolesSeeder>();
        var adminSeeder = this.ServiceProvider.GetRequiredService<AdministratorSeeder>();

        // Act:
        await rolesSeeder.SeedAsync(this.ServiceProvider);
        await adminSeeder.SeedAsync(this.ServiceProvider);
        
        await rolesSeeder.SeedAsync(this.ServiceProvider);
        await adminSeeder.SeedAsync(this.ServiceProvider);

        // Assert:
        var roleCount = await this.DbContext.Roles.CountAsync();
        Assert.Equal(3, roleCount); // "seeding roles should be an idempotent operation."

        var userCount = await this.DbContext.Users.CountAsync();
        Assert.Equal(1, userCount); // "seeding the administrator should be an idempotent operation."
    }
}