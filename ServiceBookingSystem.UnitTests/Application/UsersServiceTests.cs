using Microsoft.AspNetCore.Identity;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Application.Services;

namespace ServiceBookingSystem.UnitTests.Application;

public class UsersServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UsersService _usersService;

    public UsersServiceTests()
    {
        // Use the helper — compiles and works!
        (_userManager, _roleManager, _dbContext) = IdentityTestHelper.CreateTestIdentity();

        _usersService = new UsersService(_userManager, _roleManager);

        // Seed data
        SeedData().GetAwaiter().GetResult();
    }

    private async Task SeedData()
    {
        await CreateRoleIfNotExists("Admin");
        await CreateRoleIfNotExists("Manager");
        await CreateRoleIfNotExists("Customer");

        var admin = new ApplicationUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            FirstName = "Super",
            LastName = "Admin"
        };

        var managerUser = new ApplicationUser
        {
            UserName = "manager@example.com",
            Email = "manager@example.com",
            FirstName = "Bob",
            LastName = "Manager"
        };

        var customer1 = new ApplicationUser
        {
            UserName = "cust1@example.com",
            Email = "cust1@example.com",
            FirstName = "Alice",
            LastName = "Customer"
        };

        var customer2 = new ApplicationUser
        {
            UserName = "cust2@example.com",
            Email = "cust2@example.com",
            FirstName = "Charlie",
            LastName = "Customer"
        };

        // Create users (no password needed for roles test)
        await _userManager.CreateAsync(admin, "TempPass123!");
        await _userManager.CreateAsync(managerUser, "TempPass123!");
        await _userManager.CreateAsync(customer1, "TempPass123!");
        await _userManager.CreateAsync(customer2, "TempPass123!");

        // Assign roles
        await _userManager.AddToRoleAsync(admin, "Admin");
        await _userManager.AddToRoleAsync(admin, "Manager");

        await _userManager.AddToRoleAsync(managerUser, "Manager");

        await _userManager.AddToRoleAsync(customer1, "Customer");
        await _userManager.AddToRoleAsync(customer2, "Customer");
    }

    private async Task CreateRoleIfNotExists(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            await _roleManager.CreateAsync(new ApplicationRole(roleName));
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _userManager?.Dispose();
        _roleManager?.Dispose();
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithExistingRole_ShouldReturnUsersWithAllTheirRoles()
    {
        // Act
        var result = (await _usersService.GetUsersInRoleAsync("Manager")).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        var adminDto = result.FirstOrDefault(u => u.Email == "admin@example.com");
        Assert.NotNull(adminDto);
        Assert.Equal("Super", adminDto.FirstName);
        Assert.Contains("Admin", adminDto.Roles);
        Assert.Contains("Manager", adminDto.Roles);
        Assert.Equal(2, adminDto.Roles.Count);

        var managerDto = result.FirstOrDefault(u => u.Email == "manager@example.com");
        Assert.NotNull(managerDto);
        Assert.Single(managerDto.Roles);
        Assert.Equal("Manager", managerDto.Roles[0]);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithCustomerRole_ShouldReturnOnlyCustomers()
    {
        // Act
        var result = (await _usersService.GetUsersInRoleAsync("Customer")).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, user =>
        {
            Assert.Single(user.Roles);
            Assert.Equal("Customer", user.Roles[0]);
        });
        Assert.Contains(result, u => u.Email == "cust1@example.com");
        Assert.Contains(result, u => u.Email == "cust2@example.com");
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithNonExistentRole_ShouldReturnEmpty()
    {
        // Act
        var result = await _usersService.GetUsersInRoleAsync("GhostRole");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithNullOrEmptyRole_ShouldReturnEmpty()
    {
        // Act & Assert
        var result1 = await _usersService.GetUsersInRoleAsync(null!);
        var result2 = await _usersService.GetUsersInRoleAsync("");
        var result3 = await _usersService.GetUsersInRoleAsync("   ");

        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Empty(result3);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_CaseInsensitiveRoleName_ShouldWork()
    {
        // Act
        var result = await _usersService.GetUsersInRoleAsync("admin");

        // Assert
        Assert.Single(result);
        Assert.Equal("admin@example.com", result.First().Email);
        Assert.Contains("Admin", result.First().Roles);
    }
}