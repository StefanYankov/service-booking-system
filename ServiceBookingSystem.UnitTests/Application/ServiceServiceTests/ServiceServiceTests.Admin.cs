using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    [Fact]
    public async Task GetServicesForAdminAsync_ShouldReturnAllServices_IncludingDeleted()
    {
        // Arrange
        var provider = new ApplicationUser { Id = "p1", Email = "p@test.com", FirstName = "P", LastName = "T" };
        var category = new Category { Id = 1, Name = "Cat" };
        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);

        var activeService = new Service 
        { 
            Name = "Active", 
            Description = "Desc", 
            ProviderId = "p1", 
            CategoryId = 1, 
            IsActive = true 
        };
        var inactiveService = new Service 
        { 
            Name = "Inactive", 
            Description = "Desc", 
            ProviderId = "p1", 
            CategoryId = 1, 
            IsActive = false 
        };
        var deletedService = new Service 
        { 
            Name = "Deleted", 
            Description = "Desc", 
            ProviderId = "p1", 
            CategoryId = 1, 
            IsDeleted = true 
        };

        await dbContext.Services.AddRangeAsync(activeService, inactiveService, deletedService);
        await dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await serviceService.GetServicesForAdminAsync(parameters);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Contains(result.Items, s => s.Name == "Active");
        Assert.Contains(result.Items, s => s.Name == "Inactive");
        Assert.Contains(result.Items, s => s.Name == "Deleted");
        Assert.True(result.Items.First(s => s.Name == "Deleted").IsDeleted);
    }

    [Fact]
    public async Task DeleteServiceByAdminAsync_ShouldSoftDeleteService_WithoutOwnershipCheck()
    {
        // Arrange
        var service = new Service 
        { 
            Name = "To Ban", 
            Description = "Desc", 
            ProviderId = "p1", 
            CategoryId = 1 
        };
        await dbContext.Services.AddAsync(service);
        await dbContext.SaveChangesAsync();

        // Act
        await serviceService.DeleteServiceByAdminAsync(service.Id);

        // Assert
        dbContext.ChangeTracker.Clear();
        var deletedService = await dbContext.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == service.Id);
        Assert.NotNull(deletedService);
        Assert.True(deletedService.IsDeleted);
    }

    [Fact]
    public async Task DeleteServiceByAdminAsync_WithInvalidId_ShouldThrowNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            serviceService.DeleteServiceByAdminAsync(999));
    }
}
