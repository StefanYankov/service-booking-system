using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;

namespace ServiceBookingSystem.IntegrationTests;

public class SeedingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;

    public SeedingTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Seeders_OnFirstRun_ShouldPopulateRolesAndAdminUser()
    {
        // Arrange
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Act
        
        // seeding happens during arrangement phase
        
        // Assert
        var adminRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Administrator);
        var providerRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Provider);
        var customerRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Customer);

        adminRoleExists.Should().BeTrue("the Administrator role is essential for system management.");
        providerRoleExists.Should().BeTrue("the Provider role is essential for offering services.");
        customerRoleExists.Should().BeTrue("the Customer role is essential for booking services.");  

        // Check that the default administrator user was created.
        var adminUser = await userManager.FindByEmailAsync("admin@servicebooking.com");
        adminUser.Should().NotBeNull("a default administrator user should be created for initial setup.");
        
        // Check that the admin user was correctly assigned to the Administrator role.
        var isAdmin = await userManager.IsInRoleAsync(adminUser!, RoleConstants.Administrator);
        isAdmin.Should().BeTrue("the default user must be assigned the Administrator role.");
    }
    
    [Fact]
    public async Task Seeders_WhenRunMultipleTimes_ShouldBeIdempotent()
    {
        // ARRANGE
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var rolesSeeder = serviceProvider.GetRequiredService<RolesSeeder>();
        var adminSeeder = serviceProvider.GetRequiredService<AdministratorSeeder>();

        // ACT
        await rolesSeeder.SeedAsync(serviceProvider);
        await adminSeeder.SeedAsync(serviceProvider);

        // ASSERT
        
        // The database should still contain exactly three roles.
        var roleCount = await dbContext.Roles.CountAsync();
        roleCount.Should().Be(3, "seeding roles should be an idempotent operation.");

        // The database should still contain exactly one user.
        var userCount = await dbContext.Users.CountAsync();
        userCount.Should().Be(1, "seeding the administrator should be an idempotent operation.");
    }
}