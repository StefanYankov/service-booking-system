using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class MvcProfileControllerTests : BaseIntegrationTest
{
    public MvcProfileControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Get_Index_ReturnsViewWithProfileData()
    {
        // Arrange
        var user = await SeedUserAsync();
        var client = CreateAuthenticatedClient(user.Id);

        // Act
        var response = await client.GetAsync("/Profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(user.FirstName, content);
        Assert.Contains(user.Email!, content);
    }

    [Fact]
    public async Task Post_Update_ValidData_RedirectsToIndex()
    {
        // Arrange
        var user = await SeedUserAsync();
        var client = CreateAuthenticatedClient(user.Id);

        var formData = new Dictionary<string, string>
        {
            { "Id", user.Id },
            { "FirstName", "UpdatedName" },
            { "LastName", "UpdatedLast" },
            { "Email", user.Email! },
            { "PhoneNumber", "1234567890" }
        };

        // Act
        var response = await client.PostAsync("/Profile/Update", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Verify DB
        DbContext.ChangeTracker.Clear();
        var dbUser = await DbContext.Users.FindAsync(user.Id);
        Assert.Equal("UpdatedName", dbUser!.FirstName);
    }

    [Fact]
    public async Task Post_Update_InvalidData_ReturnsViewWithErrors()
    {
        // Arrange
        var user = await SeedUserAsync();
        var client = CreateAuthenticatedClient(user.Id);

        var formData = new Dictionary<string, string>
        {
            { "Id", user.Id },
            { "FirstName", "" }, // Invalid: Required
            { "LastName", "Valid" },
            { "Email", user.Email! }
        };

        // Act
        var response = await client.PostAsync("/Profile/Update", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Redisplays form
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("The First Name field is required", content);
    }

    [Fact]
    public async Task Post_Update_OtherUserProfile_ReturnsForbidden()
    {
        // Arrange
        var attacker = await SeedUserAsync();
        var victim = await SeedUserAsync();
        var client = CreateAuthenticatedClient(attacker.Id); // Logged in as Attacker

        var formData = new Dictionary<string, string>
        {
            { "Id", victim.Id }, // Trying to update Victim
            { "FirstName", "Hacked" },
            { "LastName", "Hacked" },
            { "Email", victim.Email! }
        };

        // Act
        var response = await client.PostAsync("/Profile/Update", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChangePassword_ValidData_RedirectsToIndex()
    {
        // Arrange
        var user = await SeedUserAsync();
        var client = CreateAuthenticatedClient(user.Id);

        var formData = new Dictionary<string, string>
        {
            { "OldPassword", "Password123!" },
            { "NewPassword", "NewPassword123!" },
            { "ConfirmPassword", "NewPassword123!" }
        };

        // Act
        var response = await client.PostAsync("/Profile/ChangePassword", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbUser = await userManager.FindByIdAsync(user.Id);
        var check = await userManager.CheckPasswordAsync(dbUser!, "NewPassword123!");
        Assert.True(check);
    }

    [Fact]
    public async Task Post_ChangePassword_Mismatch_ReturnsViewWithErrors()
    {
        // Arrange
        var user = await SeedUserAsync();
        var client = CreateAuthenticatedClient(user.Id);

        var formData = new Dictionary<string, string>
        {
            { "OldPassword", "Password123!" },
            { "NewPassword", "NewPassword123!" },
            { "ConfirmPassword", "Mismatch!" }
        };

        // Act
        var response = await client.PostAsync("/Profile/ChangePassword", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("The new password and confirmation password do not match", content);
    }

    // --- Helpers ---

    private HttpClient CreateAuthenticatedClient(string userId)
    {
        var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

                services.AddSingleton<IAntiforgery, MockAntiforgery>();
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        return client;
    }

    private async Task<ApplicationUser> SeedUserAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"u_{Guid.NewGuid()}@test.com", Email = $"u_{Guid.NewGuid()}@test.com", FirstName = "F", LastName = "L" };
        await userManager.CreateAsync(user, "Password123!");
        return user;
    }
}