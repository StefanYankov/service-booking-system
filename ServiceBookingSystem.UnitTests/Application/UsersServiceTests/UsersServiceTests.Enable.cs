using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

public partial class UsersServiceTests
{
    [Fact]
    public async Task EnableUserAsync_WhenUserIsDisabled_ShouldEnable()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "disabled@test.com",
            Email = "disabled@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Disable

        // Act:
        var result = await usersService.EnableUserAsync(user.Id);

        // Assert:
        Assert.True(result.Succeeded);
        var enabledUser = await userManager.FindByIdAsync(user.Id);
        Assert.Null(enabledUser!.LockoutEnd);
    }

    [Fact]
    public async Task EnableUserAsync_WhenUserIsNotDisabled_ShouldDoNothingAndReturnSuccess()
    {
        // Arrange:
        var user = new ApplicationUser
        {
            UserName = "active@test.com",
            Email = "active@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, "Password123!");

        // Act:
        var result = await usersService.EnableUserAsync(user.Id);

        // Assert:
        Assert.True(result.Succeeded);
        var enabledUser = await userManager.FindByIdAsync(user.Id);
        Assert.Null(enabledUser!.LockoutEnd);
    }

    [Fact]
    public async Task EnableUserAsync_WhenUserNotFound_ShouldThrowEntityNotFoundException()
    {
        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            usersService.EnableUserAsync("non-existent-id"));
    }
}