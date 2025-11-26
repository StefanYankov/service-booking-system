using Moq;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

public partial class UsersServiceTests
{
    [Fact]
    public async Task DisableUserAsync_WithActiveUser_ShouldLockAccountAndSendEmail()
    {
        // Arrange:
        var userToDisable = await userManager.FindByEmailAsync("cust1@example.com");
        Assert.NotNull(userToDisable);

        var isInitiallyLockedOut = await userManager.IsLockedOutAsync(userToDisable);
        Assert.False(isInitiallyLockedOut);

        templateServiceMock
            .Setup(s => s.RenderTemplateAsync("AccountDisabled.html", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("Fake disabled account email body");

        // Act:
        var result = await usersService.DisableUserAsync(userToDisable.Id);

        // Assert:
        Assert.True(result.Succeeded);
        var userAfterDisable = await userManager.FindByIdAsync(userToDisable.Id);
        Assert.NotNull(userAfterDisable);
        Assert.True(await userManager.IsLockedOutAsync(userAfterDisable));
        Assert.Equal(DateTimeOffset.MaxValue, userAfterDisable.LockoutEnd);

        emailServiceMock.Verify(
            service => service.SendEmailAsync(
                userToDisable.Email,
                "Account Disabled Notification",
                "Fake disabled account email body"
            ),
            Times.Once
        );
    }
    
    [Fact]
    public async Task DisableUserAsync_WithAlreadyLockedUser_ShouldSucceedAndNotSendEmail()
    {
        // Arrange:
        var userToLock = await userManager.FindByEmailAsync("cust2@example.com");
        Assert.NotNull(userToLock);
        await userManager.SetLockoutEndDateAsync(userToLock, DateTimeOffset.MaxValue);
        Assert.True(await userManager.IsLockedOutAsync(userToLock));

        // Act:
        var result = await usersService.DisableUserAsync(userToLock.Id);

        // Assert:
        Assert.True(result.Succeeded);

        emailServiceMock.Verify(
            service => service.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never
        );
    }
    
    [Fact]
    public async Task DisableUserAsync_WithNonExistentUserId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const string nonExistentId = "this-id-does-not-exist";

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => usersService.DisableUserAsync(nonExistentId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u200B")]
    [InlineData("this-id-does-not-exist")]
    public async Task DisableUserAsync_WithEmptyOrWhitespaceUserId_ShouldThrowArgumentException(string invalidId)
    {
        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => usersService.DisableUserAsync(invalidId));
    }
}