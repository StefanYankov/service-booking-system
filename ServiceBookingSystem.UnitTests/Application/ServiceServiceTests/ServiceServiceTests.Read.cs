using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    [Fact]
    public async Task GetServiceByIdAsync_WithExistingId_ShouldReturnCorrectlyMappedDto()
    {
        // Arrange:
        const string providerId = "provider-id";
        const int serviceId = 1;

        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "John",
            LastName = "Doe"
        };
        var category = new Category
        {
            Id = 1,
            Name = "Test Category"
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Description",
            Price = 100,
            DurationInMinutes = 60,
            IsOnline = true,
            IsActive = true,
            StreetAddress = "123 Main St",
            City = "Test City",
            PostalCode = "12345",
            ProviderId = providerId,
            CategoryId = 1
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(serviceId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.Id);
        Assert.Equal("Test Service", result.Name);
        Assert.Equal("Description", result.Description);
        Assert.Equal(100, result.Price);
        Assert.Equal(60, result.DurationInMinutes);
        Assert.True(result.IsOnline);
        Assert.True(result.IsActive);
        Assert.Equal("123 Main St", result.StreetAddress);
        Assert.Equal("Test City", result.City);
        Assert.Equal("12345", result.PostalCode);
        Assert.Equal(providerId, result.ProviderId);
        Assert.Equal("John Doe", result.ProviderName);
        Assert.Equal(1, result.CategoryId);
        Assert.Equal("Test Category", result.CategoryName);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithSoftDeletedService_ShouldReturnNull()
    {
        // Arrange:
        const string providerId = "provider-id";
        const int serviceId = 2;

        var service = new Service
        {
            Id = serviceId,
            Name = "Deleted Service",
            Description = "This service is deleted.",
            ProviderId = providerId,
            CategoryId = 1,
            IsDeleted = true, // Soft deleted
            DeletedOn = DateTime.UtcNow
        };
        

        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "John",
            LastName = "Doe"
        };
        var category = new Category
        {
            Id = 1,
            Name = "Test Category"
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();
        
        this.dbContext.ChangeTracker.Clear();

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(serviceId);

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange:
        const int nonExistentId = 999;

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(nonExistentId);

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithNegativeId_ShouldReturnNull()
    {
        // Arrange:
        const int negativeId = -1;

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(negativeId);

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceByIdAsync_ShouldNotTrackEntity()
    {
        // Arrange:
        const string providerId = "provider-id";
        const int serviceId = 3;

        var provider = new ApplicationUser { Id = providerId, FirstName = "John", LastName = "Doe" };
        var category = new Category { Id = 1, Name = "Test Category" };
        var service = new Service
        {
            Id = serviceId,
            Name = "Tracking Test Service",
            Description = "Test Description",
            ProviderId = providerId,
            CategoryId = 1,
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        this.dbContext.ChangeTracker.Clear();

        // Act:
        var result = await this.serviceService.GetServiceByIdAsync(serviceId);

        // Assert:
        Assert.NotNull(result);
        
        
        var isTracked = this.dbContext.ChangeTracker.Entries<Service>()
            .Any(e => e.Entity.Id == serviceId); // Verify that the ChangeTracker is NOT tracking the service entity
            
        Assert.False(isTracked, "Entity should not be tracked when using AsNoTracking()");
    }
}