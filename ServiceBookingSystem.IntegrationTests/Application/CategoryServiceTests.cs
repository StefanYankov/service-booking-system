using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Entities.Domain;
using Xunit;

namespace ServiceBookingSystem.IntegrationTests.Application;

public class CategoryServiceTests : BaseIntegrationTest
{
    public CategoryServiceTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateAsync_WhenCalledThroughApplicationHost_ShouldPersistEntity()
    {
        // Arrange:
        var service = this.ServiceProvider.GetRequiredService<ICategoryService>();

        var dto = new CategoryCreateDto
        {
            Name = "E2E Test Category",
            Description = "Integration Test"
        };

        // Act:
        var createdCategory = await service.CreateAsync(dto);

        // Assert:
        this.DbContext.ChangeTracker.Clear();
        var categoryInDb = await this.DbContext.Categories.FindAsync(createdCategory.Id);

        Assert.NotNull(categoryInDb);
        Assert.Equal("E2E Test Category", categoryInDb.Name);
    }

    [Fact]
    public async Task GetByIdAsync_AfterDeleting_ShouldReturnNullDueToGlobalQueryFilter()
    {
        // Arrange:
        var categoryToDelete = new Category
        {
            Name = "Category to Delete"
        };
        this.DbContext.Categories.Add(categoryToDelete);
        await this.DbContext.SaveChangesAsync();
        
        this.DbContext.ChangeTracker.Clear();

        // Act:
        var service = this.ServiceProvider.GetRequiredService<ICategoryService>();
        await service.DeleteAsync(categoryToDelete.Id);

        // Assert:
        var result = await service.GetByIdAsync(categoryToDelete.Id);

        Assert.Null(result);
    }
}