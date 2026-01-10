using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class UsersControllerTests : BaseIntegrationTest
{
    public UsersControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Register_ValidCustomer_ShouldReturnToken()
    {
        // Arrange:
        var dto = new RegisterDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "newuser@test.com",
            Password = "Password123!",
            Role = "Customer"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        Assert.NotNull(result?.Token);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange:
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existingUser = new ApplicationUser
        {
            UserName = "existing@test.com",
            Email = "existing@test.com",
            FirstName = "Existing",
            LastName = "User"
        };
        await userManager.CreateAsync(existingUser, "Password123!");

        var dto = new RegisterDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "existing@test.com",
            Password = "Password123!",
            Role = "Customer"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidModel_ShouldReturnBadRequest()
    {
        // Arrange:
        var dto = new RegisterDto
        {
            FirstName = "",
            LastName = "User",
            Email = "not-an-email",
            Password = "123",
            Role = "Customer"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        // Deserialize as ValidationProblemDetails to access Errors
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        
        // Verify that validation errors are present
        Assert.Contains("FirstName", problemDetails.Errors.Keys);
        Assert.Contains("Email", problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task GetMyProfile_Authorized_ShouldReturnProfile()
    {
        // Arrange
        const string email = "profile@test.com";
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Profile",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "Password123!");

        var token = await GetAuthTokenAsync(email, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.GetAsync("/api/users/me");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserViewDto>();
        Assert.Equal(email, profile?.Email);
        Assert.Equal("Profile", profile?.FirstName);
    }

    [Fact]
    public async Task UpdateMyProfile_ValidData_ShouldUpdateDb()
    {
        // Arrange:
        const string email = "update@test.com";
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Original",
            LastName = "Name"
        };
        await userManager.CreateAsync(user, "Password123!");

        var token = await GetAuthTokenAsync(email, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new UserUpdateDto
        {
            Id = user.Id,
            Email = email,
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "1234567890"
        };

        // Act
        var response = await this.Client.PutAsJsonAsync("/api/users/me", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify DB
        // Clear tracker to ensure we fetch fresh data from DB, not cache
        this.DbContext.ChangeTracker.Clear();
        
        var updatedUser = await userManager.FindByEmailAsync(email);
        Assert.Equal("Updated", updatedUser?.FirstName);
        Assert.Equal("1234567890", updatedUser?.PhoneNumber);
    }

    [Fact]
    public async Task ChangePassword_ValidData_ShouldSucceed()
    {
        // ArrangeL
        const string email = "changepass@test.com";
        const string oldPass = "OldPass123!";
        const string newPass = "NewPass123!";
        
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, oldPass);

        var token = await GetAuthTokenAsync(email, oldPass);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ChangePasswordDto
        {
            OldPassword = oldPass,
            NewPassword = newPass,
            ConfirmNewPassword = newPass
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/auth/change-password", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify login with new password
        var loginResponse = await this.Client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = newPass });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_ShouldReturnBadRequest()
    {
        // Arrange:
        const string email = "wrongpass@test.com";
        const string oldPass = "OldPass123!";
        
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, oldPass);

        var token = await GetAuthTokenAsync(email, oldPass);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ChangePasswordDto
        {
            OldPassword = "WrongPassword!",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/auth/change-password", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_ShouldSucceed()
    {
        // Arrange:
        const string email = "confirm@test.com";
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "Password123!");
        
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var dto = new ConfirmEmailDto
        {
            UserId = user.Id,
            Code = token
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/auth/confirm-email", dto);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        this.DbContext.ChangeTracker.Clear();
        var confirmedUser = await userManager.FindByEmailAsync(email);
        Assert.True(confirmedUser?.EmailConfirmed);
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