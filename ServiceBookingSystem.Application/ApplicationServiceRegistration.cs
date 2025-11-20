using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Application.Services;

namespace ServiceBookingSystem.Application;

/// <summary>
/// A static class for registering application-layer services with the dependency injection container.
/// </summary>
public static class ApplicationServiceRegistration
{
    /// <summary>
    /// Adds all the application-layer services to the specified IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <returns>The same IServiceCollection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        // services.AddScoped<IServiceService, ServiceService>(); // Future
        // services.AddScoped<IReviewService, ReviewService>();   // Future

        return services;
    }
}