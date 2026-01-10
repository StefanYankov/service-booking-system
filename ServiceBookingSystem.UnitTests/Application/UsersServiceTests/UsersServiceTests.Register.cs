using Microsoft.AspNetCore.Identity;
using Moq;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

public partial class UsersServiceTests
{
    [Fact]
    public async Task RegisterUserAsync_WithValidCustomer_ShouldSucceed()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "new_register@example.com",
            Password = "Password123!",
            Role = "Customer"
        };

        // Act
        var result = await usersService.RegisterUserAsync(dto);

        // Assert
        Assert.True(result.Succeeded);

        // Verify User Created
        var user = await userManager.FindByEmailAsync(dto.Email);
        Assert.NotNull(user);
        Assert.Equal(dto.FirstName, user.FirstName);
        
        // Verify Role Assigned
        var isInRole = await userManager.IsInRoleAsync(user, "Customer");
        Assert.True(isInRole);

        // Verify Email Sent
        emailServiceMock.Verify(x => x.SendEmailAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidProvider_ShouldSucceed()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "Provider",
            Email = "new_provider@example.com",
            Password = "Password123!",
            Role = "Provider"
        };

        // Act
        var result = await usersService.RegisterUserAsync(dto);

        // Assert
        Assert.True(result.Succeeded);
        var user = await userManager.FindByEmailAsync(dto.Email);
        Assert.NotNull(user);
        var isInRole = await userManager.IsInRoleAsync(user, "Provider");
        Assert.True(isInRole);
    }

    [Fact]
    public async Task RegisterUserAsync_WithInvalidRole_ShouldFail()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "hacker@example.com",
            Password = "Password123!",
            Role = "Administrator" // Not allowed
        };

        // Act
        var result = await usersService.RegisterUserAsync(dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "InvalidRole");
        
        // Verify User NOT Created
        var user = await userManager.FindByEmailAsync(dto.Email);
        Assert.Null(user);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenEmailExists_ShouldReturnErrors()
    {
        // Arrange
        // Create existing user first
        var existingUser = new ApplicationUser { UserName = "existing@example.com", Email = "existing@example.com", FirstName = "Ex", LastName = "User" };
        await userManager.CreateAsync(existingUser, "Password123!");

        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "existing@example.com", // Duplicate
            Password = "Password123!",
            Role = "Customer"
        };

        // Act
        var result = await usersService.RegisterUserAsync(dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "DuplicateUserName");
    }

    [Fact]
    public async Task RegisterUserAsync_WithWeakPassword_ShouldFail()
    {
        // Arrange
        // Identity options usually enforce length/complexity. 
        // Assuming default options (Digit, Uppercase, NonAlphanumeric, Length 6).
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "weakpass@example.com",
            Password = "123", // Too short
            Role = "Customer"
        };

        // Act
        var result = await usersService.RegisterUserAsync(dto);

        // Assert
        Assert.False(result.Succeeded);
        // Error code depends on Identity options, usually "PasswordTooShort"
        Assert.Contains(result.Errors, e => e.Code.StartsWith("Password"));
    }

    [Fact]
    public async Task RegisterUserAsync_WhenEmailServiceFails_ShouldStillSucceed()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "email_fail@example.com",
            Password = "Password123!",
            Role = "Customer"
        };

        // Mock email service to throw
        emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP Error"));

        // Act
        var result = await usersService.RegisterUserAsync(dto);

        // Assert
        Assert.True(result.Succeeded, "Registration should succeed even if email fails (non-blocking)");
        var user = await userManager.FindByEmailAsync(dto.Email);
        Assert.NotNull(user);
    }
}