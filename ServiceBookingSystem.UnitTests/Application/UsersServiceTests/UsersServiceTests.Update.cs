using Moq;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

public partial class UsersServiceTests
{
        [Fact]
    public async Task UpdateUserAsync_WhenEmailIsUnchanged_ShouldUpdatePropertiesAndNotSendEmail()
    {
        // Arrange:
        var userToUpdate = await this.userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(userToUpdate);
        var updateDto = new UserUpdateDto
        {
            Id = userToUpdate.Id,
            FirstName = "Updated" + userToUpdate.FirstName,
            LastName = "Updated" + userToUpdate.LastName,
            Email = userToUpdate.Email!,
            PhoneNumber = userToUpdate.PhoneNumber,
        };

        // Act:
        var result = await usersService.UpdateUserAsync(updateDto);

        // Assert:
        Assert.True(result.Succeeded);
        var updatedUser = await userManager.FindByIdAsync(userToUpdate.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(updateDto.FirstName, updatedUser.FirstName);
        Assert.Equal(updateDto.LastName, updatedUser.LastName);
        Assert.Equal(updateDto.PhoneNumber, updatedUser.PhoneNumber);
        Assert.Equal(updateDto.Email, updatedUser.Email);

        emailServiceMock.Verify(
            service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailIsChanged_ShouldUpdatePropertiesAndTriggerVerificationEmail()
    {
        // Arrange:
        var userToUpdate = await this.userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(userToUpdate);
        const string updatedEmail = "updated_admin@example.com";
        var updateDto = new UserUpdateDto
        {
            Id = userToUpdate.Id,
            FirstName = "Updated" + userToUpdate.FirstName,
            LastName = userToUpdate.LastName,
            Email = updatedEmail,
            PhoneNumber = userToUpdate.PhoneNumber,
        };

        templateServiceMock
            .Setup(s => s.RenderTemplateAsync("ConfirmEmail.html", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("Test placeholder confirmation email body");

        // Act:
        var result = await usersService.UpdateUserAsync(updateDto);

        // Assert:
        Assert.True(result.Succeeded);
        var updatedUser = await userManager.FindByIdAsync(userToUpdate.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(updateDto.FirstName, updatedUser.FirstName);
        Assert.Equal(updateDto.LastName, updatedUser.LastName);
        Assert.Equal(updateDto.PhoneNumber, updatedUser.PhoneNumber);
        Assert.Equal(updatedEmail, updatedUser.Email);
        Assert.Equal(updatedEmail, updatedUser.UserName);

        emailServiceMock.Verify(
            service => service.SendEmailAsync(updatedEmail,
                "Confirm your new email",
                "Test placeholder confirmation email body"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange:
        UserUpdateDto nullDto = null;

        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => usersService.UpdateUserAsync(nullDto));
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUserId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var nonExistentId = Guid.NewGuid();
        var updateDto = new UserUpdateDto
        {
            Id = nonExistentId.ToString(),
            FirstName = "Non-existent first name",
            LastName = "Non-existent last name",
            Email = "Non-existent@example.com",
            PhoneNumber = "1234567890",
        };
        
        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => usersService.UpdateUserAsync(updateDto));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailCaseChanges_ShouldSucceedAndNotSendEmail()
    {
        // Arrange:
        var userToUpdate = await userManager.FindByEmailAsync("cust1@example.com");
        Assert.NotNull(userToUpdate);

        var updateDto = new UserUpdateDto
        {
            Id = userToUpdate.Id,
            FirstName = "CasingTest",
            LastName = userToUpdate.LastName,
            Email = "Cust1@Example.com", 
            PhoneNumber = userToUpdate.PhoneNumber,
        };

        // Act:
        var result = await usersService.UpdateUserAsync(updateDto);

        // Assert:
        Assert.True(result.Succeeded);

        var userAfterUpdate = await userManager.FindByIdAsync(userToUpdate.Id);
        Assert.NotNull(userAfterUpdate);
        Assert.Equal("CasingTest", userAfterUpdate.FirstName);

        emailServiceMock.Verify(
            service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenAddingANewRole_ShouldSucceed()
    {
        // Arrange:
        var userFromDb = await userManager.FindByEmailAsync("cust1@example.com");
        // cust1 has "Customer". Add "Provider".
        var rolesToAdd = new List<string> { RoleConstants.Provider, RoleConstants.Customer };

        // Act:
        var result = await usersService.UpdateUserRolesAsync(userFromDb.Id, rolesToAdd);

        // Assert:
        Assert.True(result.Succeeded);
        var updatedUser = await userManager.FindByIdAsync(userFromDb.Id);
        Assert.NotNull(updatedUser);

        var roles = await userManager.GetRolesAsync(updatedUser);
        Assert.Contains(RoleConstants.Customer, roles);
        Assert.Contains(RoleConstants.Provider, roles);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenRemovingARole_ShouldSucceed()
    {
        // Arrange:
        var user = await userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(user);
        // Admin has "Administrator". Change to "Customer".
        var newRoles = new List<string> { RoleConstants.Customer };

        // Act:
        var result = await usersService.UpdateUserRolesAsync(user.Id, newRoles);

        // Assert:
        Assert.True(result.Succeeded);
        var finalRoles = await userManager.GetRolesAsync(user);
        var singleRole = Assert.Single(finalRoles);
        Assert.Equal(RoleConstants.Customer, singleRole);
        Assert.DoesNotContain(RoleConstants.Administrator, finalRoles);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenReplacingAllRoles_ShouldSucceed()
    {
        // Arrange:
        var user = await userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(user);

        var newRoles = new List<string> { RoleConstants.Customer };

        // Act
        var result = await usersService.UpdateUserRolesAsync(user.Id, newRoles);

        // Assert
        Assert.True(result.Succeeded);
        var finalRoles = await userManager.GetRolesAsync(user);
        var singleRole = Assert.Single(finalRoles);
        Assert.Equal(RoleConstants.Customer, singleRole);
        Assert.DoesNotContain(RoleConstants.Administrator, finalRoles);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithAnEmptyRoleList_ShouldRemoveAllRoles()
    {
        // Arrange
        var user = await userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(user);

        var newRoles = new List<string>();

        // Act
        var result = await usersService.UpdateUserRolesAsync(user.Id, newRoles);

        // Assert
        Assert.True(result.Succeeded);
        var finalRoles = await userManager.GetRolesAsync(user);
        Assert.Empty(finalRoles);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithNonExistentUserId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const string nonExistentId = "this-id-does-not-exist";
        var roles = new List<string> { RoleConstants.Customer };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            usersService.UpdateUserRolesAsync(nonExistentId, roles));
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithInvalidRoleNameInList_ShouldFailAndReturnError()
    {
        // Arrange:
        var user = await userManager.FindByEmailAsync("cust1@example.com");
        Assert.NotNull(user);
        var initialRoles = await userManager.GetRolesAsync(user);

        var newRoles = new List<string> { RoleConstants.Customer, "NonExistentRole" };

        // Act:
        var result = await usersService.UpdateUserRolesAsync(user.Id, newRoles);

        // Assert:
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "RoleNotFound");

        var finalRoles = await userManager.GetRolesAsync(user);
        Assert.Equal(initialRoles.Count, finalRoles.Count);
        Assert.Equal(initialRoles.First(), finalRoles.First());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u200B")]
    [InlineData("this-id-does-not-exist")]
    public async Task UpdateUserRolesAsync_WithAnyInvalidId_ShouldThrowEntityNotFoundException(string invalidId)
    {
        // Arrange:
        var roles = new List<string> { RoleConstants.Customer };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => usersService.UpdateUserRolesAsync(invalidId, roles));
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithNullRoleList_ShouldThrowArgumentNullException()
    {
        // Arrange:
        var user = await userManager.FindByEmailAsync("cust1@example.com");
        Assert.NotNull(user);
        List<string> nullRoles = null;

        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => usersService.UpdateUserRolesAsync(user.Id, nullRoles));
    }
}