using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Application.Services;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
public partial class UsersServiceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<ApplicationRole> roleManager;
    private readonly Mock<IEmailService> emailServiceMock;
    private readonly Mock<ITemplateService> templateServiceMock;
    private readonly IConfiguration configuration;
    private readonly UsersService usersService;
    private readonly ILogger<UsersService> logger;


    public UsersServiceTests()
    {
        (userManager, roleManager, dbContext) = IdentityTestHelper.CreateTestIdentity();

        this.emailServiceMock = new Mock<IEmailService>();
        this.templateServiceMock = new Mock<ITemplateService>();
        this.logger = NullLogger<UsersService>.Instance; 
        
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "WebAppSettings:BaseUrl", "http://localhost:7045" }
        };

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        usersService = new UsersService(
            userManager,
            roleManager,
            emailServiceMock.Object,
            templateServiceMock.Object,
            configuration,
            logger);

        SeedData().GetAwaiter().GetResult();
    }

    private async Task SeedData()
    {
        await CreateRoleIfNotExists(RoleConstants.Administrator);
        await CreateRoleIfNotExists(RoleConstants.Provider);
        await CreateRoleIfNotExists(RoleConstants.Customer);

        var admin = new ApplicationUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            FirstName = "Super",
            LastName = "Admin"
        };

        var provider = new ApplicationUser
        {
            UserName = "provider@example.com",
            Email = "provider@example.com",
            FirstName = "Bob",
            LastName = "Provider"
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
        await userManager.CreateAsync(admin, "TempPass123!");
        await userManager.CreateAsync(provider, "TempPass123!");
        await userManager.CreateAsync(customer1, "TempPass123!");
        await userManager.CreateAsync(customer2, "TempPass123!");

        // Assign roles
        await userManager.AddToRoleAsync(admin, RoleConstants.Administrator);
        await userManager.AddToRoleAsync(provider, RoleConstants.Provider);
        await userManager.AddToRoleAsync(customer1, RoleConstants.Customer);
        await userManager.AddToRoleAsync(customer2, RoleConstants.Customer);
    }

    private async Task CreateRoleIfNotExists(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new ApplicationRole(roleName));
    }

    public void Dispose()
    {
        dbContext?.Dispose();
        userManager?.Dispose();
        roleManager?.Dispose();
    }
   
}