namespace ServiceBookingSystem.Core.Contracts;

/// <summary>
/// Defines the contract for a data seeder.
/// Seeders are responsible for populating the database with initial or essential data.
/// This pattern allows for multiple, decoupled seeders to be discovered and run automatically
/// during application startup.
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Asynchronously seeds data into the application.
    /// </summary>
    /// <param name="serviceProvider">
    /// An <see cref="IServiceProvider"/> which can be used to resolve any services
    /// required by the seeder, such as a DbContext, UserManager, or RoleManager.
    /// </param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous seed operation.</returns>
    Task SeedAsync(IServiceProvider serviceProvider);
}
