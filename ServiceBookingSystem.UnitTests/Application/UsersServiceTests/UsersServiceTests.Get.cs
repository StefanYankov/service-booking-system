using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.UsersServiceTests;

public partial class UsersServiceTests
{
        // --- Paging tests --- \\\
    [Fact]
    public async Task GetAllUsersAsync_WhenRequestingFirstPage_ShouldReturnFirstPageOfUsers()
    {
        // Arrange:
        for (int i = 4; i < 15; i++)
        {
            var user = new ApplicationUser
            {
                FirstName = $"Test{i}",
                LastName = $"User{i}",
                Email = $"test{i:D2}@example.com",
                UserName = $"test{i:D2}@example.com"
            };

            var createResult = await userManager.CreateAsync(user, "Password123!");
            if (!createResult.Succeeded)
            {
                throw new Exception($"Test setup failed: Could not create user {i}.");
            }

            await userManager.AddToRoleAsync(user, RoleConstants.Customer);
        }

        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("admin@example.com", result.Items.First().Email);
        Assert.Equal("test09@example.com", result.Items.Last().Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenRequestingSecondPage_ShouldReturnSecondPageOfUsers()
    {
        // Arrange:
        for (int i = 4; i < 15; i++)
        {
            var user = new ApplicationUser
            {
                FirstName = $"Test{i}",
                LastName = $"User{i}",
                Email = $"test{i:D2}@example.com",
                UserName = $"test{i:D2}@example.com"
            };

            var createResult = await userManager.CreateAsync(user, "Password123!");
            if (!createResult.Succeeded)
            {
                throw new Exception($"Test setup failed: Could not create user {i}.");
            }

            await userManager.AddToRoleAsync(user, RoleConstants.Customer);
        }

        var parameters = new UserQueryParameters
        {
            PageNumber = 2,
            PageSize = 10,
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal("test10@example.com", result.Items.First().Email);
        Assert.Equal("test14@example.com", result.Items.Last().Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenNoUsersExist_ShouldReturnEmptyPagedResult()
    {
        // Arrange:
        var users = await userManager.Users.ToListAsync();
        foreach (var user in users)
        {
            await userManager.DeleteAsync(user);
        }

        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithPageNumberBeyondTotalPages_ShouldReturnEmptyItems()
    {
        // Arrange:
        var parameters = new UserQueryParameters
        {
            PageNumber = 10,
            PageSize = 10
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithZeroPageSize_ShouldReturnEmptyItems()
    {
        // Arrange:
        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 0
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(4, result.TotalCount);
    }

    // --- Sorting tests --- \\\
    [Fact]
    public async Task GetAllUsersAsync_WhenSortingByEmailDescending_ShouldReturnUsersInCorrectOrder()
    {
        // Arrange:
        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "email",
            SortDirection = "desc"
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.Items.Count);
        Assert.Equal("provider@example.com", result.Items[0].Email);
        Assert.Equal("cust2@example.com", result.Items[1].Email);
        Assert.Equal("cust1@example.com", result.Items[2].Email);
        Assert.Equal("admin@example.com", result.Items[3].Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithDefaultSort_ShouldSortByUsernameAscending()
    {
        // Arrange:
        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal("admin@example.com", result.Items[0].Email);
        Assert.Equal("cust1@example.com", result.Items[1].Email);
        Assert.Equal("cust2@example.com", result.Items[2].Email);
        Assert.Equal("provider@example.com", result.Items[3].Email);
    }

    // --- Filtering tests --- \\\

    [Fact]
    public async Task GetAllUsersAsync_WithSearchTermMatchingOneUser_ShouldReturnOnlyThatUser()
    {
        // Arrange:
        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "alic"
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        var user = Assert.Single(result.Items);
        Assert.Equal("Alice", user.FirstName);
        Assert.Equal("cust1@example.com", user.Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSearchTermMatchingMultipleUsers_ShouldReturnAllMatchingUsers()
    {
        // Arrange:
        var user1 = new ApplicationUser
            { FirstName = "John", LastName = "Smith", UserName = "john@s.com", Email = "john@s.com" };
        var user2 = new ApplicationUser
            { FirstName = "Jane", LastName = "Smith", UserName = "jane@s.com", Email = "jane@s.com" };
        await userManager.CreateAsync(user1, "Password123!");
        await userManager.CreateAsync(user2, "Password123!");

        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "smith"
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, u => u.FirstName == "John");
        Assert.Contains(result.Items, u => u.FirstName == "Jane");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSearchTermMatchingNoUsers_ShouldReturnEmptyResult()
    {
        // Arrange:
        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "NonExistentNameAbc123"
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithWhitespaceSearchTerm_ShouldReturnAllUsers()
    {
        // Arrange:
        var parameters = new UserQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "   "
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.Items.Count);
    }

    // --- Combined tests --- \\\
    [Fact]
    public async Task GetAllUsersAsync_WithFilterAndSortAndPaging_ShouldReturnCorrectSubset()
    {
        // Arrange:
        for (int i = 0; i < 12; i++)
        {
            var user = new ApplicationUser
            {
                FirstName = $"Person{i:D2}",
                LastName = (i % 2 == 0) ? "Searchable" : $"Other{i}",
                Email = $"person{i:D2}@example.com",
                UserName = $"person{i:D2}@example.com"
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        var parameters = new UserQueryParameters
        {
            SearchTerm = "searchable",
            SortBy = "email",
            SortDirection = "desc",
            PageNumber = 2,
            PageSize = 4
        };

        // Act:
        var result = await usersService.GetAllUsersAsync(parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(4, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("person02@example.com", result.Items.First().Email);
        Assert.Equal("person00@example.com", result.Items.Last().Email);
    }
    [Fact]
    public async Task GetUserByIdAsync_WithExistingId_ShouldReturnCorrectUserDto()
    {
        // Arrange:
        var existingUser = await userManager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(existingUser);
        var id = existingUser.Id;

        // Act: 
        var result = await usersService.GetUserByIdAsync(id);
        // Assert:

        Assert.NotNull(result);
        Assert.Equal(existingUser.Email, result.Email);
        Assert.Equal(existingUser.FirstName, result.FirstName);
        Assert.Contains(RoleConstants.Administrator, result.Roles);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange:
        var invalidId = Guid.NewGuid();

        // Act:
        var result = await usersService.GetUserByIdAsync(invalidId.ToString());

        // Assert:
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u200B")]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull(string? invalidId)
    {
        // Act:
        var result = await usersService.GetUserByIdAsync(invalidId!);

        // Assert:
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetUsersInRoleAsync_WithExistingRole_ShouldReturnUsersWithAllTheirRoles()
    {
        // Act
        var result = (await usersService.GetUsersInRoleAsync(RoleConstants.Provider)).ToList();

        // Assert
        Assert.Single(result); // Only 1 provider seeded

        var providerDto = result.FirstOrDefault(u => u.Email == "provider@example.com");
        Assert.NotNull(providerDto);
        Assert.Equal("Bob", providerDto.FirstName);
        Assert.Single(providerDto.Roles);
        Assert.Equal(RoleConstants.Provider, providerDto.Roles[0]);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithCustomerRole_ShouldReturnOnlyCustomers()
    {
        // Act
        var result = (await usersService.GetUsersInRoleAsync(RoleConstants.Customer)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, user =>
        {
            Assert.Single(user.Roles);
            Assert.Equal(RoleConstants.Customer, user.Roles[0]);
        });
        Assert.Contains(result, u => u.Email == "cust1@example.com");
        Assert.Contains(result, u => u.Email == "cust2@example.com");
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithNonExistentRole_ShouldReturnEmpty()
    {
        // Act
        var result = await usersService.GetUsersInRoleAsync("GhostRole");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_WithNullOrEmptyRole_ShouldReturnEmpty()
    {
        // Act & Assert
        var result1 = await usersService.GetUsersInRoleAsync(null!);
        var result2 = await usersService.GetUsersInRoleAsync("");
        var result3 = await usersService.GetUsersInRoleAsync("   ");

        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Empty(result3);
    }

    [Fact]
    public async Task GetUsersInRoleAsync_CaseInsensitiveRoleName_ShouldWork()
    {
        // Act
        var result = (await usersService.GetUsersInRoleAsync(RoleConstants.Administrator.ToLower())).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("admin@example.com", result.First().Email);
        Assert.Contains(RoleConstants.Administrator, result.First().Roles);
    }
}