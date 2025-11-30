using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Data.Contexts;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
public partial class ServiceServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> dbContextOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<ServiceService> logger;
    private readonly Mock<IUsersService> usersServiceMock;
    private readonly Mock<ICategoryService> categoryServiceMock;
    private readonly ServiceService serviceService;

    public ServiceServiceTests()
    {
        // --- ARRANGE (Common Setup) ---
        this.dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ServiceBookingTestDb_{Guid.NewGuid()}")
            .Options;
        this.dbContext = new ApplicationDbContext(dbContextOptions);
        this.logger = NullLogger<ServiceService>.Instance;

        this.usersServiceMock = new Mock<IUsersService>();
        this.categoryServiceMock = new Mock<ICategoryService>();

        this.serviceService = new ServiceService(
            dbContext,
            logger,
            usersServiceMock.Object,
            categoryServiceMock.Object
        );
    }

    /// <summary>
    /// This is a cleanup method that xUnit runs after each test has finished.
    /// It ensures that the in-memory database is deleted after each test,
    /// providing a clean slate for the next one.
    /// </summary>
    public void Dispose()
    {
        // The 'Dispose' method on the context will clean up the in-memory database.
        dbContext?.Dispose();
    }



    /// <summary>
    /// This is a private "stunt double" for our real DbContext.
    /// It inherits from ApplicationDbContext so it has all the same DbSets.
    /// We override SaveChangesAsync to force it to throw an exception for testing purposes.
    /// </summary>
    private class FailingDbContext : ApplicationDbContext
    {
        public FailingDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Overriding the method to simulate a failure.
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Returning a Task that immediately throws the exception we want to test against.
            return Task.FromException<int>(new DbUpdateException("Simulated database save error."));
        }
    }
}