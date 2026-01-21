using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class CategoryControllerTests : BaseIntegrationTest
{
    public CategoryControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task GetAll_ShouldReturnCategories_Publicly()
    {
        // Arrange
        await SeedCategoryAsync("Public Cat");

        // Act
        var response = await this.Client.GetAsync("/api/categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CategoryViewDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, c => c.Name == "Public Cat");
    }

    [Fact]
    public async Task Create_AsAdmin_ShouldReturnCreated()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CategoryCreateDto { Name = "New Cat", Description = "Desc" };

        // Act
        var response = await this.Client.PostAsJsonAsync("/api/categories", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CategoryViewDto>();
        Assert.Equal("New Cat", result!.Name);
    }

    [Fact]
    public async Task Create_AsCustomer_ShouldReturnForbidden()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var token = await GetAuthTokenAsync(customer.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CategoryCreateDto { Name = "Hacked Cat", Description = "Desc" };

        // Act
        var response = await this.Client.PostAsJsonAsync("/api/categories", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsAdmin_ShouldUpdateCategory()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var category = await SeedCategoryAsync("Old Name");
        var dto = new CategoryUpdateDto { Id = category.Id, Name = "New Name", Description = "New Desc" };

        // Act
        var response = await this.Client.PutAsJsonAsync($"/api/categories/{category.Id}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CategoryViewDto>();
        Assert.Equal("New Name", result!.Name);
    }

    [Fact]
    public async Task Delete_AsAdmin_ShouldDeleteCategory()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var category = await SeedCategoryAsync("To Delete");

        // Act
        var response = await this.Client.DeleteAsync($"/api/categories/{category.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Categories.FindAsync(category.Id);
        Assert.Null(deleted); // Should be null if soft-deleted and query filter is on, or hard deleted?
        // CategoryService uses dbContext.Categories.Remove(categoryToDelete);
        // Category is NOT IDeletableEntity (it inherits BaseEntity<int>).
        // Wait, let me check Category definition.
    }

    // --- Helpers ---

    private async Task<ApplicationUser> SeedAdminAsync()
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "admin@test.com", Email = "admin@test.com", FirstName = "Admin", LastName = "User" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Administrator);
        return user;
    }

    private async Task<ApplicationUser> SeedCustomerAsync()
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "cust@test.com", Email = "cust@test.com", FirstName = "Cust", LastName = "User" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Customer);
        return user;
    }

    private async Task<Category> SeedCategoryAsync(string name)
    {
        var category = new Category { Name = name, Description = "Desc" };
        await DbContext.Categories.AddAsync(category);
        await DbContext.SaveChangesAsync();
        return category;
    }

    private async Task<string> GetAuthTokenAsync(string email, string password)
    {
        var loginResponse = await this.Client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return loginResult!.Token;
    }

    private class LoginResult
    {
        public string Token { get; init; } = null!;
    }
}
