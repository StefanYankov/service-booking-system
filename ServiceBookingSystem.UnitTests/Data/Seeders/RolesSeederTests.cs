using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;

namespace ServiceBookingSystem.UnitTests.Data.Seeders;

public class RolesSeederTests
{
    private readonly Mock<RoleManager<ApplicationRole>> mockRoleManager;
    private readonly Mock<ILogger<RolesSeeder>> mockLogger;
    private readonly Mock<IServiceProvider> mockServiceProvider;
    private readonly RolesSeeder rolesSeeder;

    public RolesSeederTests()
    {
        // --- ARRANGE (Common Setup) ---

        var mockRoleStore = new Mock<IRoleStore<ApplicationRole>>();
        mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
            mockRoleStore.Object, 
            null!, // optionsAccessor
            null!, // passwordHasher
            null!, // userValidators
            null!, // passwordValidators
            null!, // keyNormalizer
            null!, // errors
            null!, // services
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object); // logger

        mockLogger = new Mock<ILogger<RolesSeeder>>();

        mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(RoleManager<ApplicationRole>)))
            .Returns(mockRoleManager.Object);
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ILogger<RolesSeeder>)))
            .Returns(mockLogger.Object);

        rolesSeeder = new RolesSeeder();
    }

    [Fact]
    public async Task SeedAsync_WhenRolesDoNotExist_ShouldCreateAllRoles()
    {
        // ARRANGE 
        mockRoleManager
            .Setup(rm => rm.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        mockRoleManager
            .Setup(rm => rm.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        // ACT
        await rolesSeeder.SeedAsync(mockServiceProvider.Object);

        // ASSERT
        mockRoleManager.Verify(rm => rm.CreateAsync(It.Is<ApplicationRole>(r => r.Name == RoleConstants.Administrator)), Times.Once);
        mockRoleManager.Verify(rm => rm.CreateAsync(It.Is<ApplicationRole>(r => r.Name == RoleConstants.Provider)), Times.Once);
        mockRoleManager.Verify(rm => rm.CreateAsync(It.Is<ApplicationRole>(r => r.Name == RoleConstants.Customer)), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenRolesAlreadyExist_ShouldNotCreateAnyRoles()
    {
        // ARRANGE (Test-Specific Setup)
        mockRoleManager
            .Setup(rm => rm.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // ACT
        await rolesSeeder.SeedAsync(mockServiceProvider.Object);

        // ASSERT
        mockRoleManager.Verify(rm => rm.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }
}