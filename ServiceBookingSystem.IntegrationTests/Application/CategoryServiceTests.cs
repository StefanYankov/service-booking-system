using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.IntegrationTests.Application;

public class CategoryServiceTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;

    public CategoryServiceTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateAsync_WhenCalledThroughApplicationHost_ShouldPersistEntity()
    {
        // Arrange:
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICategoryService>();

        var dto = new CategoryCreateDto
        {
            Name = "E2E Test Category",
            Description = "Integration Test"
        };

        // Act:
        var createdCategory = await service.CreateAsync(dto);

        // Assert:
        using var assertScope = factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var categoryInDb = await assertContext.Categories.FindAsync(createdCategory.Id);

        Assert.NotNull(categoryInDb);
        Assert.Equal("E2E Test Category", categoryInDb.Name);
    }

    [Fact]
    public async Task GetByIdAsync_AfterDeleting_ShouldReturnNullDueToGlobalQueryFilter()
    {
        // Arrange:
        using var arrangeScope = factory.Services.CreateScope();
        var arrangeContext = arrangeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var categoryToDelete = new Category
        {
            Name = "Category to Delete"
        };
        arrangeContext.Categories.Add(categoryToDelete);
        await arrangeContext.SaveChangesAsync();

        // Act:
        using var actScope = factory.Services.CreateScope();
        var service = actScope.ServiceProvider.GetRequiredService<ICategoryService>();
        await service.DeleteAsync(categoryToDelete.Id);

        // Assert:
        using var assertScope = factory.Services.CreateScope();
        var assertService = assertScope.ServiceProvider.GetRequiredService<ICategoryService>();

        var result = await assertService.GetByIdAsync(categoryToDelete.Id);

        Assert.Null(result);
    }
}