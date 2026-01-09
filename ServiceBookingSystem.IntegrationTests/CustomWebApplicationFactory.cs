using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Contexts;
using Testcontainers.MsSql;

namespace ServiceBookingSystem.IntegrationTests;

/// <summary>
/// A custom WebApplicationFactory for integration tests.
/// This factory spins up a real SQL Server Docker container using Testcontainers.
/// It replaces the application's database configuration to point to this container.
/// </summary>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly MsSqlContainer dbContainer = new MsSqlBuilder()
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Remove the existing DbContext configuration (from Program.cs).
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 2. Add DbContext pointing to the Testcontainer.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(this.dbContainer.GetConnectionString());
            });
        });
    }

    /// <summary>
    /// Starts the container before any tests run.
    /// </summary>
    public async Task InitializeAsync()
    {
        await this.dbContainer.StartAsync();
        
        // Ensure the database is created (Apply Migrations).
        using var scope = this.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Stops the container after all tests finish.
    /// Explicit implementation to avoid conflict with WebApplicationFactory.DisposeAsync.
    /// </summary>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await this.dbContainer.StopAsync();
    }
}