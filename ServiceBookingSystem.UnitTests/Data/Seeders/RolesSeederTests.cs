using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;
using Microsoft.EntityFrameworkCore;

namespace ServiceBookingSystem.UnitTests.Data.Seeders;

public class RolesSeederTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly ApplicationDbContext dbContext;

    public RolesSeederTests()
    {
        // --- ARRANGE (Common Setup using a real in-memory Identity setup) ---

        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddSingleton<ILogger<RolesSeeder>>(NullLogger<RolesSeeder>.Instance);
    
        services.AddSingleton<ILogger<RoleManager<ApplicationRole>>>(NullLogger<RoleManager<ApplicationRole>>.Instance);

        serviceProvider = services.BuildServiceProvider();
        dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task SeedAsync_WhenRolesDoNotExist_ShouldCreateAllRoles()
    {
        // Arrange:
        var seeder = new RolesSeeder();

        // Act:
        await seeder.SeedAsync(serviceProvider);

        // Assert:
        Assert.True(await dbContext.Roles.AnyAsync(r => r.Name == RoleConstants.Administrator));
        Assert.True(await dbContext.Roles.AnyAsync(r => r.Name == RoleConstants.Provider));
        Assert.True(await dbContext.Roles.AnyAsync(r => r.Name == RoleConstants.Customer));
    }

    [Fact]
    public async Task SeedAsync_WhenRolesAlreadyExist_ShouldNotCreateDuplicates()
    {
        // Arrange:
        var seeder = new RolesSeeder();
        
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        await roleManager.CreateAsync(new ApplicationRole(RoleConstants.Administrator));

        // Act:
        await seeder.SeedAsync(serviceProvider);

        // Assert:
        Assert.True(await dbContext.Roles.AnyAsync(r => r.Name == RoleConstants.Administrator));
        Assert.True(await dbContext.Roles.AnyAsync(r => r.Name == RoleConstants.Provider));
        Assert.True(await dbContext.Roles.AnyAsync(r => r.Name == RoleConstants.Customer));
        
        Assert.Equal(1, await dbContext.Roles.CountAsync(r => r.Name == RoleConstants.Administrator));
        Assert.Equal(3, await dbContext.Roles.CountAsync());
    }
    
    public void Dispose()
    {
        serviceProvider.Dispose();
        dbContext.Dispose();
    }
}