using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

public partial class UsersServiceTests
{
    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ShouldSucceed()
    {
        // Arrange:
        var user = new ApplicationUser
        {
            UserName = "pass_user@test.com",
            Email = "pass_user@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "OldPass123!");

        var dto = new ChangePasswordDto
        {
            OldPassword = "OldPass123!",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        };

        // Act:
        var result = await usersService.ChangePasswordAsync(user.Id, dto);

        // Assert:
        Assert.True(result.Succeeded);
        var checkOld = await userManager.CheckPasswordAsync(user, "OldPass123!");
        var checkNew = await userManager.CheckPasswordAsync(user, "NewPass123!");
        Assert.False(checkOld);
        Assert.True(checkNew);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongOldPassword_ShouldFail()
    {
        // Arrange:
        var user = new ApplicationUser
        {
            UserName = "wrong_pass@test.com",
            Email = "wrong_pass@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "OldPass123!");

        var dto = new ChangePasswordDto
        {
            OldPassword = "WrongPass!",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        };

        // Act:
        var result = await usersService.ChangePasswordAsync(user.Id, dto);

        // Assert:
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordMismatch");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWeakNewPassword_ShouldFail()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "weak_pass@test.com", Email = "weak_pass@test.com", FirstName = "Test", LastName = "User" };
        await userManager.CreateAsync(user, "OldPass123!");

        var dto = new ChangePasswordDto
        {
            OldPassword = "OldPass123!",
            NewPassword = "123", // Too short
            ConfirmNewPassword = "123"
        };

        // Act
        var result = await usersService.ChangePasswordAsync(user.Id, dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code.StartsWith("Password"));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var dto = new ChangePasswordDto
        {
            OldPassword = "Old",
            NewPassword = "New",
            ConfirmNewPassword = "New"
        };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            usersService.ChangePasswordAsync("non-existent-id", dto));
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidToken_ShouldSucceed()
    {
        // Arrange:
        var user = new ApplicationUser
        {
            UserName = "confirm@test.com",
            Email = "confirm@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        
        await userManager.CreateAsync(user, "Pass123!");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var dto = new ConfirmEmailDto
        {
            UserId = user.Id,
            Code = token
        };

        // Act:
        var result = await usersService.ConfirmEmailAsync(dto);

        // Assert:
        Assert.True(result.Succeeded);
        var confirmedUser = await userManager.FindByIdAsync(user.Id);
        Assert.True(confirmedUser!.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithInvalidToken_ShouldFail()
    {
        // Arrange:
        var user = new ApplicationUser
        {
            UserName = "invalid_token@test.com",
            Email = "invalid_token@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "Pass123!");

        var dto = new ConfirmEmailDto
        {
            UserId = user.Id,
            Code = "invalid-token"
        };

        // Act:
        var result = await usersService.ConfirmEmailAsync(dto);

        // Assert:
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "InvalidToken");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithNonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var dto = new ConfirmEmailDto
        {
            UserId = "non-existent",
            Code = "token"
        };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            usersService.ConfirmEmailAsync(dto));
    }
}