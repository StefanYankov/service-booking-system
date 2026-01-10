using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Seeders;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests;

/// <summary>
/// Base class for all integration tests.
/// Handles database resetting (Respawn) to ensure a clean state for every test method.
/// </summary>
[Collection("Integration Tests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory<Program> factory;
    private readonly IServiceScope scope;
    private static Respawner? respawner;
    private static string? connectionString;
    
    protected readonly HttpClient Client;
    protected readonly ApplicationDbContext DbContext;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ITestOutputHelper Output;

    protected BaseIntegrationTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.Output = output;
        
        // Wire up the logger. 
        // The factory implements ITestOutputHelperAccessor, so setting this property
        // directs the MartinCostello.Logging.XUnit logger to the current test's output.
        this.factory.OutputHelper = output;
        
        this.Client = factory.CreateClient();
        
        // Create a scope for the test method to resolve scoped services (like DbContext).
        this.scope = factory.Services.CreateScope();
        this.ServiceProvider = this.scope.ServiceProvider;
        this.DbContext = this.scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        // Ensure the logger is set for this test run
        this.factory.OutputHelper = this.Output;

        // Initialize Respawner once per factory instance (lazy loading).
        if (respawner == null)
        {
            connectionString = this.DbContext.Database.GetConnectionString();
            if (connectionString != null)
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    TablesToIgnore = ["__EFMigrationsHistory"],
                    SchemasToInclude = ["dbo"],
                    DbAdapter = DbAdapter.SqlServer
                });
            }
        }

        // Reset the database before the test runs.
        if (respawner != null && connectionString != null)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await respawner.ResetAsync(connection);
        }
        
        // Re-seed essential data (Roles) because Respawn wiped them.
        var rolesSeeder = this.ServiceProvider.GetRequiredService<RolesSeeder>();
        await rolesSeeder.SeedAsync(this.ServiceProvider);
    }

    public async Task DisposeAsync()
    {
        this.scope.Dispose();
        // Clear output to prevent writing to disposed helper
        this.factory.OutputHelper = null;
    }
}