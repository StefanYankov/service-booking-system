using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.AvailabilityServiceTests;

public partial class AvailabilityServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> dbContextOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly AvailabilityService availabilityService;

    public AvailabilityServiceTests()
    {
        dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"AvailabilityTests_{Guid.NewGuid()}")
            .Options;
        dbContext = new ApplicationDbContext(dbContextOptions);
        var logger = new Mock<ILogger<AvailabilityService>>().Object;
        availabilityService = new AvailabilityService(dbContext, logger);
    }

    private async Task SeedServiceAndProvider()
    {
        var provider = new ApplicationUser { 
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var category = new Category
        {
            Id = 1,
            Name = "Test Category"
        };
        
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Desc",
            DurationInMinutes = 60,
            ProviderId = "provider-1",
            CategoryId = 1
        };

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);
        await dbContext.SaveChangesAsync();
    }
    
    public void Dispose()
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Dispose();
    }
}
