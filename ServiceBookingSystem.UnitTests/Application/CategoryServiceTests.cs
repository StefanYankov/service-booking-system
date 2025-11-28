using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
namespace ServiceBookingSystem.UnitTests.Application;

public class CategoryServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> dbContextOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly CategoryService categoryService;
    private readonly ILogger<CategoryService> logger;

    public CategoryServiceTests()
    {
        // --- ARRANGE (Common Setup) ---
        dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ServiceBookingTestDb_{Guid.NewGuid()}")
            .Options;

        dbContext = new ApplicationDbContext(dbContextOptions);
        this.logger = NullLogger<CategoryService>.Instance;
        categoryService = new CategoryService(dbContext, logger);
    }

    /// <summary>
    /// This is a cleanup method that xUnit runs after each test has finished.
    /// It ensures that the in-memory database is deleted after each test,
    /// providing a clean slate for the next one.
    /// </summary>
    public void Dispose()
    {
        // The 'Dispose' method on the context will clean up the in-memory database.
        dbContext.Dispose();
    }

    // --- CreateAsync Tests ---
    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateCategoryAndReturnDto()
    {
        // Arrange:
        var validDto = new CategoryCreateDto
        {
            Name = "Electronics",
            Description = "Gadgets and devices"
        };

        // Act:
        var resultDto = await categoryService.CreateAsync(validDto);

        //Assert:
        Assert.NotNull(resultDto);
        Assert.True(resultDto.Id > 0);
        Assert.Equal(validDto.Name, resultDto.Name);

        var categoryInDb = await dbContext.Categories.FindAsync(resultDto.Id);
        Assert.NotNull(categoryInDb);
        Assert.Equal(validDto.Name, categoryInDb.Name);
        Assert.Equal(validDto.Description, categoryInDb.Description);
    }

    [Fact]
    public async Task CreateAsync_WithExistingName_ShouldThrowDuplicateEntityException()
    {
        // Arrange:
        var validDto = new CategoryCreateDto
        {
            Name = "Electronics",
            Description = "Gadgets and devices"
        };

        await categoryService.CreateAsync(validDto);

        var duplicateDto = new CategoryCreateDto
        {
            Name = "Electronics",
            Description = "Gadgets and devices"
        };


        // Act & Assert
        var ex = await Assert.ThrowsAsync<DuplicateEntityException>(() => categoryService.CreateAsync(duplicateDto));
        Assert.Contains("Electronics", ex.Message);
    }

    // --- Read tests ---
    // --- GetByIdAsync tests ---

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCorrectCategoryDto()
    {
        // Arrange:
        var categoryEntity = new Category
        {
            Name = "Electronics",
            Description = "Gadgets and devices"
        };
        dbContext.Categories.Add(categoryEntity);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await categoryService.GetByIdAsync(categoryEntity.Id);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(categoryEntity.Name, result.Name);
        Assert.Equal(categoryEntity.Description, result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange:
        const int nonExistentId = 999;

        // Act:
        var result = await categoryService.GetByIdAsync(nonExistentId);

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryIsSoftDeleted_ShouldReturnNull()
    {
        // Arrange:
        var category = new Category
        {
            Name = "Electronics",
            Description = "Gadgets and devices"
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        await categoryService.DeleteAsync(category.Id);

        // Act:
        //  clean context and service to simulate a separate request.
        await using var assertContext = new ApplicationDbContext(dbContextOptions);
        var assertLogger = NullLogger<CategoryService>.Instance; 
        var assertService = new CategoryService(assertContext, assertLogger);

        var result = await assertService.GetByIdAsync(category.Id);

        // Assert:
        Assert.Null(result);
    }

    // --- GetAllAsync tests---

    [Fact]
    public async Task GetAllAsync_WhenPaging_ShouldReturnCorrectPageOfItems()
    {
        // Arrange:
        var allCategories = new List<Category>();
        for (int i = 1; i <= 25; i++)
        {
            allCategories.Add(new Category { Name = $"Category {i}", Description = $"Description {i}" });
        }

        await dbContext.Categories.AddRangeAsync(allCategories);
        await dbContext.SaveChangesAsync();

        // Define the parameters for our specific test case.
        // We want to fetch the second page, with 10 items per page.
        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act:
        var pagedResult = await categoryService.GetAllAsync(parameters);

        // Assert:
        Assert.NotNull(pagedResult);
        Assert.Equal(25, pagedResult.TotalCount); // Should find all 25 items in total.
        Assert.Equal(2, pagedResult.PageNumber); // Should confirm we are on page 2.
        Assert.Equal(10, pagedResult.PageSize); // Should confirm the page size is 10.
        Assert.Equal(3, pagedResult.TotalPages); // 25 items / 10 per page = 3 pages.

        // We verify the content of the items on the current page.
        Assert.NotNull(pagedResult.Items);
        Assert.Equal(10, pagedResult.Items.Count); // This page should have exactly 10 items.

        // The default sort order is by ID. Since we added them in order, we can predict the content.
        // Page 2 should contain items 11 through 20.
        Assert.Equal("Category 11", pagedResult.Items.First().Name);
        Assert.Equal("Category 20", pagedResult.Items.Last().Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenSortingByNameDescending_ShouldReturnSortedItems()
    {
        // Arrange:
        // Add categories to the database in a non-alphabetical order.
        await dbContext.Categories.AddRangeAsync(
            new Category { Name = "B - Second" },
            new Category { Name = "C - Third" },
            new Category { Name = "A - First" }
        );
        await dbContext.SaveChangesAsync();

        // Define the parameters to sort by name in descending order.
        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name",
            SortDirection = "desc"
        };

        // Act:
        var pagedResult = await categoryService.GetAllAsync(parameters);

        // Assert:
        Assert.NotNull(pagedResult);
        Assert.Equal(3, pagedResult.Items.Count);

        // Desc sorting Z -> A.
        Assert.Equal("C - Third", pagedResult.Items[0].Name);
        Assert.Equal("B - Second", pagedResult.Items[1].Name);
        Assert.Equal("A - First", pagedResult.Items[2].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenItemsAreSoftDeleted_ShouldOnlyReturnActiveItems()
    {
        // Arrange:
        var activeCategory1 = new Category
        {
            Name = "Active Category 1"
        };
        var deletedCategory = new Category
        {
            Name = "Deleted Category",
            IsDeleted = true,
            DeletedOn = DateTime.UtcNow
        };
        var activeCategory2 = new Category
        {
            Name = "Active Category 2"
        };

        await dbContext.Categories.AddRangeAsync(activeCategory1, deletedCategory, activeCategory2);
        await dbContext.SaveChangesAsync();

        // Act:
        await using var assertContext = new ApplicationDbContext(dbContextOptions);
        var assertLogger = NullLogger<CategoryService>.Instance;
        var assertService = new CategoryService(assertContext,assertLogger);
        var parameters = new PagingAndSortingParameters();
        var result = await assertService.GetAllAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.DoesNotContain(result.Items, c => c.Name == "Deleted Category");
        Assert.Contains(result.Items, c => c.Name == "Active Category 1");
        Assert.Contains(result.Items, c => c.Name == "Active Category 2");
    }

    // --- UpdateAsync Tests ---

    [Fact]
    public async Task UpdateAsync_WithExistingId_ShouldUpdateCategory()
    {
        // Arrange:
        var originalCategory = new Category
        {
            Name = "Electronics",
            Description = "Gadgets and devices"
        };
        dbContext.Categories.Add(originalCategory);
        await dbContext.SaveChangesAsync();

        var idOfEntityToUpdate = originalCategory.Id;
        var updateDto = new CategoryUpdateDto
        {
            Id = idOfEntityToUpdate,
            Name = "Electronics - Updated",
            Description = "Gadgets and devices"
        };

        // Act:
        await categoryService.UpdateAsync(updateDto);

        // Assert:  
        var updatedCategoryInDb = await dbContext.Categories.FindAsync(originalCategory.Id);
        Assert.NotNull(updatedCategoryInDb);
        Assert.Equal("Electronics - Updated", updatedCategoryInDb.Name);
        Assert.Equal("Gadgets and devices", updatedCategoryInDb.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var updateDto = new CategoryUpdateDto
        {
            Id = 999,
            Name = "Does not exist"
        };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => categoryService.UpdateAsync(updateDto));
    }

    [Fact]
    public async Task UpdateAsync_WhenChangingToAnExistingName_ShouldThrowDuplicateEntityException()
    {
        // Arrange :
        var categoryA = new Category { Name = "Category A" };
        var categoryB = new Category { Name = "Category B" };
        await dbContext.Categories.AddRangeAsync(categoryA, categoryB);
        await dbContext.SaveChangesAsync();
        var updateDto = new CategoryUpdateDto
        {
            Id = categoryB.Id,
            Name = "Category A" // Duplicate name
        };

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateEntityException>(() => categoryService.UpdateAsync(updateDto));
    }

    // --- DeleteAsync Tests ---

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldSoftDeleteCategory()
    {
        // Arrange:
        var category = new Category
        {
            Name = "To Be Deleted"
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        // Act:
        await categoryService.DeleteAsync(category.Id);

        // Assert:
        // To correctly test the global query filter, we must use a new, clean DbContext instance.
        // This simulates a new request trying to fetch the data after it has been deleted.
        await using var assertContext = new ApplicationDbContext(dbContextOptions);
        var foundCategory = await assertContext.Categories.FindAsync(category.Id);
        Assert.Null(foundCategory); // This will now pass!

        var softDeletedCategory = await assertContext.Categories
            .IgnoreQueryFilters() // This special method bypasses the soft-delete filter for verification.
            .FirstOrDefaultAsync(c => c.Id == category.Id);

        Assert.NotNull(softDeletedCategory);
        Assert.True(softDeletedCategory.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var nonExistentId = 999;

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => categoryService.DeleteAsync(nonExistentId));
    }
}