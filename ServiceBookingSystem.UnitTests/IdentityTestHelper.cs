using Microsoft.Extensions.Logging;

namespace ServiceBookingSystem.UnitTests;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;

public static class IdentityTestHelper
{
    public static (UserManager<ApplicationUser>, RoleManager<ApplicationRole>, ApplicationDbContext) CreateTestIdentity()
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
                // Relax password rules for tests
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
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
}