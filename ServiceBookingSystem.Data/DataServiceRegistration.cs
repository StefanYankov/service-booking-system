using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Seeders;

namespace ServiceBookingSystem.Data;

/// <summary>
/// A static class for registering data-layer services with the dependency injection container.
/// </summary>
public static class DataServiceRegistration
{
    /// <summary>
    /// Adds all the data-layer services, such as seeders, to the specified IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <returns>The same IServiceCollection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddTransient<RolesSeeder>();
        services.AddTransient<AdministratorSeeder>();

        return services;
    }
}