using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;
using Xunit;

namespace ServiceBookingSystem.IntegrationTests.Data.Seeders;

public class SeedingTests : BaseIntegrationTest
{
    public SeedingTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Seeders_OnFirstRun_ShouldPopulateRolesAndAdminUser()
    {
        // Arrange
        // BaseIntegrationTest.InitializeAsync re-runs RolesSeeder after Respawn.
        // However, AdminSeeder is NOT re-run in BaseIntegrationTest.
        // So we expect Roles to exist, but maybe not Admin User unless we seed it here.
        
        // Let's check Roles first.
        var roleManager = this.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        
        // Act & Assert
        var adminRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Administrator);
        var providerRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Provider);
        var customerRoleExists = await roleManager.RoleExistsAsync(RoleConstants.Customer);

        adminRoleExists.Should().BeTrue("the Administrator role is essential for system management.");
        providerRoleExists.Should().BeTrue("the Provider role is essential for offering services.");
        customerRoleExists.Should().BeTrue("the Customer role is essential for booking services.");  

        // For Admin User, since Respawn wiped it and we didn't re-seed it in BaseIntegrationTest,
        // we should manually seed it here if we want to test it, OR update BaseIntegrationTest to seed it.
        // Given this is a "Seeding Test", it makes sense to run the seeder explicitly.
        
        var adminSeeder = this.ServiceProvider.GetRequiredService<AdministratorSeeder>();
        await adminSeeder.SeedAsync(this.ServiceProvider);
        
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync("admin@servicebooking.com");
        adminUser.Should().NotBeNull("a default administrator user should be created.");
        
        var isAdmin = await userManager.IsInRoleAsync(adminUser!, RoleConstants.Administrator);
        isAdmin.Should().BeTrue("the default user must be assigned the Administrator role.");
    }
    
    [Fact]
    public async Task Seeders_WhenRunMultipleTimes_ShouldBeIdempotent()
    {
        // ARRANGE
        var rolesSeeder = this.ServiceProvider.GetRequiredService<RolesSeeder>();
        var adminSeeder = this.ServiceProvider.GetRequiredService<AdministratorSeeder>();

        // ACT
        // Run seeders manually (Roles ran once in InitializeAsync, Admin ran 0 times)
        await rolesSeeder.SeedAsync(this.ServiceProvider);
        await adminSeeder.SeedAsync(this.ServiceProvider);
        
        // Run them AGAIN to test idempotency
        await rolesSeeder.SeedAsync(this.ServiceProvider);
        await adminSeeder.SeedAsync(this.ServiceProvider);

        // ASSERT
        var roleCount = await this.DbContext.Roles.CountAsync();
        roleCount.Should().Be(3, "seeding roles should be an idempotent operation.");

        var userCount = await this.DbContext.Users.CountAsync();
        // We expect 1 admin user.
        // Note: If other tests ran before this and created users, Respawn wiped them.
        // So only the Admin user should exist.
        userCount.Should().Be(1, "seeding the administrator should be an idempotent operation.");
    }
}