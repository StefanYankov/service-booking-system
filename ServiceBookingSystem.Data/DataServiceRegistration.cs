using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Core.Constants;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Identity;
using ServiceBookingSystem.Data.Seeders;

namespace ServiceBookingSystem.Data;

/// <summary>
/// A static class for registering data-layer services with the dependency injection container.
/// </summary>
public static class DataServiceRegistration
{
    /// <summary>
    /// Adds all data-layer services, such as DbContext, Identity, and seeders, to the specified IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="configuration">The application configuration, used for retrieving the connection string.</param>
    /// <returns>The same IServiceCollection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register ASP.NET Core Identity

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = ValidationConstraints.User.PasswordMinLength;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders(); 

        // Register data seeders
        services.AddTransient<RolesSeeder>();
        services.AddTransient<AdministratorSeeder>();
        services.AddTransient<DemoDataSeeder>();

        return services;
    }
}