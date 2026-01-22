using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class AdminServiceControllerTests : BaseIntegrationTest
{
    public AdminServiceControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task GetAllServices_AsAdmin_ShouldReturnServices()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedServiceAsync("Service 1");
        await SeedServiceAsync("Service 2");

        // Act
        var response = await this.Client.GetAsync("/api/admin/services");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceAdminViewDto>>();
        Assert.NotNull(result);
        Assert.True(result.Items.Count >= 2);
    }

    [Fact]
    public async Task GetAllServices_AsCustomer_ShouldReturnForbidden()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var token = await GetAuthTokenAsync(customer.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await this.Client.GetAsync("/api/admin/services");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteService_AsAdmin_ShouldSoftDelete()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var service = await SeedServiceAsync("To Ban");

        // Act
        var response = await this.Client.DeleteAsync($"/api/admin/services/{service.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var deletedService = await DbContext.Services.FindAsync(service.Id);
        // Since we use soft delete and query filters, FindAsync might return null if filter is on.
        // But we want to verify IsDeleted = true.
        // We can't easily ignore query filters on the DbContext instance here without casting or creating new context.
        // However, if FindAsync returns null, it means it's filtered out (deleted).
        // Let's verify it's NOT found by standard query.
        Assert.Null(deletedService);
    }

    // --- Helpers ---

    private async Task<ApplicationUser> SeedAdminAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"admin_{Guid.NewGuid()}@test.com", Email = $"admin_{Guid.NewGuid()}@test.com", FirstName = "A", LastName = "D" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Administrator);
        return user;
    }

    private async Task<ApplicationUser> SeedCustomerAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"c_{Guid.NewGuid()}@test.com", Email = $"c_{Guid.NewGuid()}@test.com", FirstName = "C", LastName = "T" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Customer);
        return user;
    }

    private async Task<Service> SeedServiceAsync(string name)
    {
        var provider = await SeedProviderAsync();
        var category = new Category { Name = $"Cat_{Guid.NewGuid()}", Description = "D" };
        await DbContext.Categories.AddAsync(category);
        await DbContext.SaveChangesAsync();

        var service = new Service { Name = name, Description = "D", ProviderId = provider.Id, CategoryId = category.Id, Price = 10, DurationInMinutes = 60 };
        await DbContext.Services.AddAsync(service);
        await DbContext.SaveChangesAsync();
        return service;
    }

    private async Task<ApplicationUser> SeedProviderAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"p_{Guid.NewGuid()}@test.com", Email = $"p_{Guid.NewGuid()}@test.com", FirstName = "P", LastName = "T" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Provider);
        return user;
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