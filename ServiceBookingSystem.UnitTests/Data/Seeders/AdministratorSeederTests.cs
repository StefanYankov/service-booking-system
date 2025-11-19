using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;

namespace ServiceBookingSystem.UnitTests.Data.Seeders;

public class AdministratorSeederTests
{
    private readonly Mock<UserManager<ApplicationUser>> mockUserManager;
    private readonly Mock<ILogger<AdministratorSeeder>> mockLogger;
    private readonly Mock<IServiceProvider> mockServiceProvider;
    private readonly AdministratorSeeder adminSeeder;

    public AdministratorSeederTests()
    {
        // ARRANGE (Common Setup)

        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, 
            null!, // optionsAccessor
            null!, // passwordHasher
            null!, // userValidators
            null!, // passwordValidators
            null!, // keyNormalizer
            null!, // errors
            null!, // services
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object); // logger

        mockLogger = new Mock<ILogger<AdministratorSeeder>>();

        mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(UserManager<ApplicationUser>)))
            .Returns(mockUserManager.Object);
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ILogger<AdministratorSeeder>)))
            .Returns(mockLogger.Object);

        adminSeeder = new AdministratorSeeder();
    }

    [Fact]
    public async Task SeedAsync_WhenNoAdminExists_ShouldCreateAdminUserAndAssignRole()
    {
        // ARRANGE (Test-Specific Setup)
        mockUserManager
            .Setup(um => um.GetUsersInRoleAsync(RoleConstants.Administrator))
            .ReturnsAsync(new List<ApplicationUser>()); // Return an empty list

        mockUserManager
            .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager
            .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleConstants.Administrator))
            .ReturnsAsync(IdentityResult.Success);

        // ACT
        await adminSeeder.SeedAsync(mockServiceProvider.Object);

        // ASSERT
        mockUserManager.Verify(um => um.CreateAsync(It.Is<ApplicationUser>(u => u.Email == "admin@servicebooking.com"), "admin123"), Times.Once);
        mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleConstants.Administrator), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminAlreadyExists_ShouldDoNothing()
    {
        // ARRANGE 
        var existingAdmin = new ApplicationUser 
        { 
            UserName = "existingadmin",
            FirstName = "Existing",
            LastName = "Admin"
        };
        mockUserManager
            .Setup(um => um.GetUsersInRoleAsync(RoleConstants.Administrator))
            .ReturnsAsync(new List<ApplicationUser> { existingAdmin }); // Return a list with one user

        // ACT
        await adminSeeder.SeedAsync(mockServiceProvider.Object);

        // ASSERT
        mockUserManager.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }
}
