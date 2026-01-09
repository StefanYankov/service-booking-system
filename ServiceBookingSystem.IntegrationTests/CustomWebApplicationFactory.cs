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
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryTestDbForSeeding");
            });
        });
    }
}