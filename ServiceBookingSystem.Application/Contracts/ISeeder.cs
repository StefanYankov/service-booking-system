using System;
using System.Threading.Tasks;

namespace ServiceBookingSystem.Application.Contracts;

/// <summary>
/// Defines the contract for a component that seeds initial data into the application.
/// This provides a standard way to populate the database with essential data on startup.
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Asynchronously executes the data seeding process.
    /// </summary>
    /// <param name="serviceProvider">
    /// An <see cref="IServiceProvider"/> used to resolve any services required for the seeding process.
    /// Using the service provider here allows the seeder implementation to be decoupled from specific
    /// services like DbContext or UserManager, as they can be resolved dynamically at runtime.
    /// </param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous seed operation.</returns>
    Task SeedAsync(IServiceProvider serviceProvider);
}
