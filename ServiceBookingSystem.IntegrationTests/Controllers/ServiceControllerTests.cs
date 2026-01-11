using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class ServiceControllerTests : BaseIntegrationTest
{
    public ServiceControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnService()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var service = new Service
        {
            Name = "Test Service",
            Description = "Desc",
            ProviderId = provider.Id,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync($"/api/service/{service.Id}");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ServiceViewDto>();
        Assert.Equal(service.Name, result!.Name);
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturn201Created()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var token = await GetAuthTokenAsync(provider.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ServiceCreateDto
        {
            Name = "New Service",
            Description = "Description",
            Price = 50,
            DurationInMinutes = 60,
            CategoryId = category.Id,
            StreetAddress = "123 Main St",
            City = "City",
            PostalCode = "12345"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/service", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ServiceViewDto>();
        Assert.Equal(dto.Name, result!.Name);
    }

    [Fact]
    public async Task Create_AsCustomer_ShouldReturn403Forbidden()
    {
        // Arrange:
        var customer = await SeedCustomerAsync();
        var category = await SeedCategoryAsync();
        var token = await GetAuthTokenAsync(customer.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ServiceCreateDto
        {
            Name = "Hacker Service",
            Description = "Desc",
            Price = 50,
            DurationInMinutes = 60,
            CategoryId = category.Id
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/service", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidModel_ShouldReturnBadRequest()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var token = await GetAuthTokenAsync(provider.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ServiceCreateDto
        {
            Name = "",
            Description = "Desc",
            Price = 50,
            DurationInMinutes = 60,
            CategoryId = 1
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/service", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Update_AsOwner_ShouldReturn200OK()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var service = new Service
        {
            Name = "Old Name",
            Description = "Desc",
            ProviderId = provider.Id,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(provider.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ServiceUpdateDto
        {
            Id = service.Id,
            Name = "Updated Name",
            Description = "Updated Desc",
            Price = 60,
            DurationInMinutes = 60,
            CategoryId = category.Id,
            IsActive = true,
            IsOnline = false
        };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/service/{service.Id}", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ServiceViewDto>();
        Assert.Equal("Updated Name", result!.Name);
    }

    [Fact]
    public async Task Update_AsOtherProvider_ShouldReturn403Forbidden()
    {
        // Arrange:
        var owner = await SeedProviderAsync("owner@test.com");
        var attacker = await SeedProviderAsync("attacker@test.com");
        var category = await SeedCategoryAsync();
        var service = new Service
        {
            Name = "Protected Service",
            Description = "Desc",
            ProviderId = owner.Id,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(attacker.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ServiceUpdateDto
        {
            Id = service.Id,
            Name = "Hacked",
            Description = "Desc",
            Price = 0,
            DurationInMinutes = 60,
            CategoryId = category.Id
        };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/service/{service.Id}", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithIdMismatch_ShouldReturnBadRequest()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var service = new Service
        {
            Name = "Service",
            Description = "Desc",
            ProviderId = provider.Id,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(provider.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ServiceUpdateDto
        {
            Id = service.Id + 1,
            Name = "Updated",
            Description = "Desc",
            Price = 60,
            DurationInMinutes = 60,
            CategoryId = category.Id
        };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/service/{service.Id}", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsOwner_ShouldReturn204NoContent()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var service = new Service
        {
            Name = "To Delete",
            Description = "Desc",
            ProviderId = provider.Id,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(provider.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.DeleteAsync($"/api/service/{service.Id}");

        // Assert:
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        this.DbContext.ChangeTracker.Clear();
        var deletedService = await this.DbContext.Services.FindAsync(service.Id);
        Assert.Null(deletedService);
    }

    [Fact]
    public async Task Delete_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var token = await GetAuthTokenAsync(provider.Email!, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.DeleteAsync("/api/service/999");

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByCategory_WithPagination_ShouldReturnPagedResult()
    {
        // Arrange:
        var category = await SeedCategoryAsync();
        var provider = await SeedProviderAsync();
        for (int i = 0; i < 15; i++)
        {
            await this.DbContext.Services.AddAsync(new Service
            {
                Name = $"Service {i}",
                Description = "Desc",
                ProviderId = provider.Id,
                CategoryId = category.Id
            });
        }
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync($"/api/service/category/{category.Id}?pageNumber=2&pageSize=10");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceViewDto>>();
        Assert.Equal(15, result!.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task Search_WithValidParams_ShouldReturnResults()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var service1 = new Service 
        {
            Name = "Plumber",
            Description = "Fix pipes",
            ProviderId = provider.Id,
            CategoryId = category.Id,
            Price = 100
        };
        
        var service2 = new Service 
        {
            Name = "Electrician",
            Description = "Fix wires",
            ProviderId = provider.Id,
            CategoryId = category.Id,
            Price = 200
        };
        await this.DbContext.Services.AddRangeAsync(service1, service2);
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync("/api/service?searchTerm=plumber&maxPrice=150");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceViewDto>>();
        Assert.Single(result!.Items);
        Assert.Equal("Plumber", result.Items.First().Name);
    }

    [Fact]
    public async Task Search_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var service1 = new Service
        {
            Name = "Plumber",
            Description = "Fix pipes",
            ProviderId = provider.Id,
            CategoryId = category.Id
        };
        
        await this.DbContext.Services.AddAsync(service1);
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync("/api/service?searchTerm=Electrician");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceViewDto>>();
        Assert.Empty(result!.Items);
    }

    [Fact]
    public async Task Search_WithCategoryId_ShouldFilterByCategory()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category1 = new Category 
        {
            Name = "Cat1",
            Description = "Desc"
        };
        
        var category2 = new Category 
        {
            Name = "Cat2",
            Description = "Desc"
        };
        
        await this.DbContext.Categories.AddRangeAsync(category1, category2);
        await this.DbContext.SaveChangesAsync();

        var service1 = new Service 
        {
            Name = "S1",
            Description = "D",
            ProviderId = provider.Id,
            CategoryId = category1.Id
        };
        
        var service2 = new Service 
        {
            Name = "S2",
            Description = "D",
            ProviderId = provider.Id,
            CategoryId = category2.Id
        };
        
        await this.DbContext.Services.AddRangeAsync(service1, service2);
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync($"/api/service?categoryId={category1.Id}");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceViewDto>>();
        Assert.Single(result!.Items);
        Assert.Equal("S1", result.Items.First().Name);
    }

    [Fact]
    public async Task Search_WithIsOnline_ShouldFilterByStatus()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var service1 = new Service 
        {
            Name = "Online",
            Description = "D",
            IsOnline = true,
            ProviderId = provider.Id,
            CategoryId = category.Id
        };
        
        var service2 = new Service 
        {
            Name = "Offline",
            Description = "D",
            IsOnline = false,
            ProviderId = provider.Id, 
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddRangeAsync(service1, service2);
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync("/api/service?isOnline=true");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceViewDto>>();
        Assert.Single(result!.Items);
        Assert.Equal("Online", result.Items.First().Name);
    }

    [Fact]
    public async Task Search_WithPagination_ShouldReturnPagedResult()
    {
        // Arrange:
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        for (int i = 0; i < 15; i++)
        {
            await this.DbContext.Services.AddAsync(new Service
            {
                Name = $"S{i}",
                Description = "D",
                ProviderId = provider.Id,
                CategoryId = category.Id
            });
        }
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync("/api/service?pageNumber=2&pageSize=5");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceViewDto>>();
        Assert.Equal(15, result!.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.Items.Count);
    }

    // --- Helpers ---

    private async Task<ApplicationUser> SeedProviderAsync(string email = "provider@test.com")
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Provider);
        return user;
    }

    private async Task<ApplicationUser> SeedCustomerAsync(string email = "customer@test.com")
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Customer);
        return user;
    }

    private async Task<Category> SeedCategoryAsync()
    {
        var category = new Category
        {
            Name = "Test Category",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();
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