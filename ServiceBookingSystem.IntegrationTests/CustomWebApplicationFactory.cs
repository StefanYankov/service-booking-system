using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Contexts;

namespace ServiceBookingSystem.IntegrationTests;

/// <summary>
/// A custom WebApplicationFactory for integration tests.
/// This factory is responsible for bootstrapping the application in-memory and overriding
/// services for the test environment, such as replacing the real database with an
/// in-memory database.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // --- Step 1: Find and Remove the real DbContext configuration ---
            // Find the descriptor for the DbContext itself.
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));

            // If the descriptor is found, we remove it from the service collection.
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // --- Step 2: Add the In-Memory Database configuration ---
            // We add a new DbContext registration, but this time we configure it
            // to use the EF Core In-Memory provider.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // "InMemoryTestDb" is the name of our in-memory database.
                // Using a unique name per test run is not strictly necessary with the factory
                // as it manages service provider lifetime, but it's a safe practice.
                options.UseInMemoryDatabase("InMemoryTestDbForSeeding");
            });
        });
    }
}