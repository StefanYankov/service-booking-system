using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class AdminControllerTests : BaseIntegrationTest
{
    public AdminControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task GetAllUsers_AsAdmin_ShouldReturnUsers()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.GetAsync("/api/admin/users");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserViewDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task GetAllUsers_AsCustomer_ShouldReturnForbidden()
    {
        // Arrange:
        var customer = await SeedCustomerAsync();
        var token = await GetAuthTokenAsync(customer.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await this.Client.GetAsync("/api/admin/users");

        // Assert:
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_WithPagination_ShouldReturnPagedResult()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Seed extra users
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        for (int i = 0; i < 15; i++)
        {
            await userManager.CreateAsync(new ApplicationUser { UserName = $"user{i}@test.com", Email = $"user{i}@test.com", FirstName = $"User{i}", LastName = "Test" }, "Password123!");
        }

        // Act
        var response = await this.Client.GetAsync("/api/admin/users?pageNumber=2&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserViewDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        // Total users = 1 Admin + 15 seeded = 16. Page 2 should have 6 items.
        Assert.Equal(6, result.Items.Count);
    }

    [Fact]
    public async Task GetAllUsers_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await userManager.CreateAsync(new ApplicationUser { UserName = "unique@test.com", Email = "unique@test.com", FirstName = "UniqueName", LastName = "Test" }, "Password123!");

        // Act
        var response = await this.Client.GetAsync("/api/admin/users?searchTerm=UniqueName");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserViewDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("UniqueName", result.Items.First().FirstName);
    }

    [Fact]
    public async Task GetAllUsers_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await userManager.CreateAsync(new ApplicationUser 
        {
            UserName = "a_user@test.com",
            Email = "a_user@test.com",
            FirstName = "A_User",
            LastName = "Test"
        }, "Password123!");
        await userManager.CreateAsync(new ApplicationUser 
        {
            UserName = "z_user@test.com",
            Email = "z_user@test.com",
            FirstName = "Z_User",
            LastName = "Test"
        }, "Password123!");

        // Act:
        var response = await this.Client.GetAsync("/api/admin/users?sortBy=email&sortDirection=desc");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserViewDto>>();
        Assert.NotNull(result);
        
        var zUserIndex = result.Items.FindIndex(u => u.Email == "z_user@test.com");
        var aUserIndex = result.Items.FindIndex(u => u.Email == "a_user@test.com");
        Assert.True(zUserIndex < aUserIndex);
    }

    [Fact]
    public async Task DisableUser_AsAdmin_ShouldSucceed()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var customer = await SeedCustomerAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.PutAsync($"/api/admin/users/{customer.Id}/disable", null);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        this.DbContext.ChangeTracker.Clear();
        var disabledUser = await this.DbContext.Users.FindAsync(customer.Id);
        Assert.True(disabledUser!.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task DisableUser_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.PutAsync("/api/admin/users/non-existent-id/disable", null);

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EnableUser_AsAdmin_ShouldSucceed()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var customer = await SeedCustomerAsync();
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await userManager.SetLockoutEndDateAsync(customer, DateTimeOffset.MaxValue);

        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.PutAsync($"/api/admin/users/{customer.Id}/enable", null);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        this.DbContext.ChangeTracker.Clear();
        var enabledUser = await this.DbContext.Users.FindAsync(customer.Id);
        Assert.Null(enabledUser!.LockoutEnd);
    }

    [Fact]
    public async Task UpdateUserRoles_AsAdmin_ShouldSucceed()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var customer = await SeedCustomerAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newRoles = new List<string> { RoleConstants.Provider };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/admin/users/{customer.Id}/roles", newRoles);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(customer.Id);
        var roles = await userManager.GetRolesAsync(updatedUser!);
        Assert.Contains(RoleConstants.Provider, roles);
        Assert.DoesNotContain(RoleConstants.Customer, roles); // Replaced
    }

    [Fact]
    public async Task UpdateUserRoles_WithEmptyList_ShouldReturnBadRequest()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var customer = await SeedCustomerAsync();
        var token = await GetAuthTokenAsync(admin.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newRoles = new List<string>();

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/admin/users/{customer.Id}/roles", newRoles);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
    }

    // --- Helpers ---

    private async Task<ApplicationUser> SeedAdminAsync()
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = "admin@test.com",
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Administrator);
        return user;
    }

    private async Task<ApplicationUser> SeedCustomerAsync()
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = "cust@test.com",
            Email = "cust@test.com",
            FirstName = "Customer",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Customer);
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