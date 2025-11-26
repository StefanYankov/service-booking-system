using Microsoft.Extensions.Logging;
using Moq;

namespace ServiceBookingSystem.UnitTests;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;

public static class IdentityTestHelper
{
    public static (UserManager<ApplicationUser>, RoleManager<ApplicationRole>, ApplicationDbContext)
        CreateTestIdentity()
    {
        var dbName = $"IdentityTestDb_{Guid.NewGuid()}";

        var services = new ServiceCollection();

        // CORRECT WAY: Pass the options builder directly
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Register Identity with your custom user and role
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Relax password rules for tests
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 0;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var serviceProvider = services.BuildServiceProvider();

        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        return (userManager, roleManager, context);
    }

    // In IdentityTestHelper.cs

    public static (Mock<UserManager<ApplicationUser>>, Mock<RoleManager<ApplicationRole>>) CreateMockedIdentity()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();

        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(um => um.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        roleManagerMock.Setup(rm => rm.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        return (userManagerMock, roleManagerMock);
    }
}