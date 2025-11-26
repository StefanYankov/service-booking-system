using Moq;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

public partial class UsersServiceTests
{
        [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldCreateUserAndAssignRoles()
    {
        // Arrange:
        var dto = new UserCreateDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "new.user@example.com",
            PhoneNumber = "0888888888",
            Password = "ValidPassword123!",
            Roles = new List<string> { "Customer" }
        };

        templateServiceMock
            .Setup(s => s.RenderTemplateAsync("ConfirmEmail.html", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("This is the fake email body.");

        // Act:
        var result = await usersService.CreateUserAsync(dto);

        // Assert;
        Assert.True(result.Succeeded);
        var userInDb = await userManager.FindByEmailAsync(dto.Email);
        Assert.NotNull(userManager);
        Assert.Equal(dto.FirstName, userInDb.FirstName);
        Assert.Equal(dto.LastName, userInDb.LastName);
        Assert.Equal(dto.Email, userInDb.Email);
        Assert.Equal(dto.PhoneNumber, userInDb.PhoneNumber);
        Assert.Equal(dto.Email, userInDb.UserName);

        var roles = await userManager.GetRolesAsync(userInDb);
        Assert.Single(roles);
        Assert.Equal("Customer", roles[0]);

        emailServiceMock.Verify(
            service => service.SendEmailAsync(
                "new.user@example.com",
                "Confirm your email",
                "This is the fake email body."
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateUserAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange:
        UserCreateDto dto = null;

        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => usersService.CreateUserAsync(dto));
    }

    [Fact]
    public async Task CreateUserAsync_WithNonExistentRole_ShouldFailAndReturnError()
    {
        // Arrange:
        const string nonExistentRole = "SuperAdmin";
        var dto = new UserCreateDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "new.user@example.com",
            PhoneNumber = "0888888888",
            Password = "ValidPassword123!",
            Roles = new List<string> { nonExistentRole }
        };

        // Act:
        var result = await usersService.CreateUserAsync(dto);

        // Assert;
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors,
            e => e.Code == "RoleNotFound"
                 && e.Description == $"The role '{nonExistentRole}' does not exist.");
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ShouldFailAndReturnError()
    {
        // Arrange:
        var existingUser = new ApplicationUser
        {
            FirstName = "Existing",
            LastName = "User",
            Email = "test.duplicate@example.com",
            UserName = "test.duplicate@example.com"
        };

        var seedResult = await userManager.CreateAsync(existingUser, "ValidPassword123!");
        Assert.True(seedResult.Succeeded);

        var duplicateUserDto = new UserCreateDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "test.duplicate@example.com",
            Password = "AnotherPassword123!",
            Roles = new List<string> { "Customer" }
        };

        // Act:
        var result = await usersService.CreateUserAsync(duplicateUserDto);

        // Assert:
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "DuplicateEmail"
                                            || e.Code == "DuplicateUserName");
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidPassword_ShouldFailAndReturnError()
    {
        // Arrange: 
        var dto = new UserCreateDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "new.user@example.com",
            PhoneNumber = "0888888888",
            Password = "123",
            Roles = new List<string> { "Customer" }
        };

        // Act:
        var result = await usersService.CreateUserAsync(dto);

        // Assert:
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordTooShort");
    }

    [Fact]
    public async Task CreateUserAsync_WithNonExistentRole_ShouldFail_AndNotSendEmail()
    {
        // Arrange:
        var nonExistentRole = "SuperAdmin";
        var dto = new UserCreateDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test.rolefail@example.com",
            Password = "Password123!",
            Roles = { nonExistentRole }
        };

        // Act:
        var result = await usersService.CreateUserAsync(dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "RoleNotFound");
        var userInDb = await userManager.FindByEmailAsync(dto.Email);
        Assert.Null(userInDb);

        emailServiceMock.Verify(
            service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateUserAsync_WhenCreationFails_ShouldNotSendEmail()
    {
        // Arrange:
        var dto = new UserCreateDto
        {
            FirstName = "Another",
            LastName = "Admin",
            Email = "admin@example.com",
            Password = "AnotherPassword123!",
            Roles = new List<string> { "Customer" }
        };

        // Act:
        var result = await usersService.CreateUserAsync(dto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "DuplicateUserName"
                                            || e.Code == "DuplicateEmail");

        emailServiceMock.Verify(
            service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

}