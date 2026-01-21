using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class MvcAdminControllerTests : BaseIntegrationTest
{
    public MvcAdminControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Get_Users_AsAdmin_ReturnsView()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var client = CreateAuthenticatedClient(admin.Id, RoleConstants.Administrator);

        // Act:
        var response = await client.GetAsync("/Admin/Admin/Users");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("User Management", content);
    }

    [Fact]
    public async Task Get_Users_AsCustomer_ReturnsForbidden()
    {
        // Arrange:
        var customer = await SeedCustomerAsync();
        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);

        // Act:
        var response = await client.GetAsync("/Admin/Admin/Users");

        // Assert:
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_DisableUser_AsAdmin_ShouldDisableAndRedirect()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var targetUser = await SeedCustomerAsync();
        var client = CreateAuthenticatedClient(admin.Id, RoleConstants.Administrator);

        var formData = new Dictionary<string, string>
        {
            { "id", targetUser.Id }
        };

        // Act:
        var response = await client.PostAsync("/Admin/Admin/DisableUser", new FormUrlEncodedContent(formData));

        // Assert:
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        DbContext.ChangeTracker.Clear(); // Clear cache to get fresh data from DB
        var userInDb = await DbContext.Users.FindAsync(targetUser.Id);
        Assert.True(userInDb!.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Post_EnableUser_AsAdmin_ShouldEnableAndRedirect()
    {
        // Arrange:
        var admin = await SeedAdminAsync();
        var targetUser = await SeedCustomerAsync();
        
        targetUser.LockoutEnd = DateTimeOffset.MaxValue;
        DbContext.Users.Update(targetUser);
        await DbContext.SaveChangesAsync();

        var client = CreateAuthenticatedClient(admin.Id, RoleConstants.Administrator);

        var formData = new Dictionary<string, string>
        {
            { "id", targetUser.Id }
        };

        // Act:
        var response = await client.PostAsync("/Admin/Admin/EnableUser", new FormUrlEncodedContent(formData));

        // Assert:
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        DbContext.ChangeTracker.Clear();
        var userInDb = await DbContext.Users.FindAsync(targetUser.Id);
        Assert.Null(userInDb!.LockoutEnd);
    }

    [Fact]
    public async Task Post_DisableUser_AsCustomer_ShouldReturnForbidden()
    {
        // Arrange:
        var customer = await SeedCustomerAsync(); // Attacker
        var targetUser = await SeedProviderAsync(); // Victim
        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);

        // Act:
        var response = await client.PostAsync("/Admin/Admin/DisableUser", new StringContent(""));

        // Assert:
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Category Tests ---

    [Fact]
    public async Task Get_Categories_AsAdmin_ReturnsView()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var client = CreateAuthenticatedClient(admin.Id, RoleConstants.Administrator);

        // Act
        var response = await client.GetAsync("/Admin/Category/Index");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Manage Categories", content);
    }

    [Fact]
    public async Task Post_CreateCategory_AsAdmin_RedirectsToIndex()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var client = CreateAuthenticatedClient(admin.Id, RoleConstants.Administrator);

        var formData = new Dictionary<string, string>
        {
            { "Name", "New MVC Cat" },
            { "Description", "Desc" }
        };

        // Act
        var response = await client.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Category", response.Headers.Location!.ToString());
        
        DbContext.ChangeTracker.Clear();
        var cat = DbContext.Categories.FirstOrDefault(c => c.Name == "New MVC Cat");
        Assert.NotNull(cat);
    }

    [Fact]
    public async Task Post_DeleteCategory_AsAdmin_RedirectsToIndex()
    {
        // Arrange
        var admin = await SeedAdminAsync();
        var category = new Category { Name = "To Delete", Description = "D" };
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var client = CreateAuthenticatedClient(admin.Id, RoleConstants.Administrator);

        var formData = new Dictionary<string, string>
        {
            { "id", category.Id.ToString() }
        };

        // Act
        var response = await client.PostAsync("/Admin/Category/Delete", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Categories.FindAsync(category.Id);
        Assert.Null(deleted);
    }

    // --- Helpers ---

    private HttpClient CreateAuthenticatedClient(string userId, string role)
    {
        var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock Auth
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

                // Mock Antiforgery to bypass token validation in tests
                services.AddSingleton<IAntiforgery, MockAntiforgery>();
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

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

    private async Task<ApplicationUser> SeedProviderAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"p_{Guid.NewGuid()}@test.com", Email = $"p_{Guid.NewGuid()}@test.com", FirstName = "P", LastName = "T" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Provider);
        return user;
    }
}