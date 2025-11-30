using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    
    [Fact]
    public async Task CreateServiceAsync_WithValidData_ShouldCreateServiceAndReturnDto()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        var createDto = new ServiceCreateDto()
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1
        };

        var providerDto = new UserViewDto()
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider",
            Roles = ["Provider"]
        };

        usersServiceMock
            .Setup(s => s.GetUserByIdAsync(providerId))
            .ReturnsAsync(providerDto);

        var categoryDto = new CategoryViewDto()
        {
            Id = createDto.CategoryId,
            Name = "Test Category"
        };

        categoryServiceMock
            .Setup(s => s.GetByIdAsync(createDto.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryDto);

        // Act:
        var resultDto = await serviceService.CreateServiceAsync(createDto, providerId);

        // Assert:
        Assert.NotNull(resultDto);
        Assert.Equal(createDto.Name, resultDto.Name);
        Assert.Equal($"{providerDto.FirstName} {providerDto.LastName}", resultDto.ProviderName);
        Assert.Equal(categoryDto.Name, resultDto.CategoryName);

        var serviceInDb = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == resultDto.Id);
        Assert.NotNull(serviceInDb);
        Assert.Equal(createDto.Name, serviceInDb.Name);
        Assert.Equal(providerId, serviceInDb.ProviderId);
    }

    [Fact]
    public async Task CreateServiceAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        ServiceCreateDto nullDto = null!;

        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => serviceService.CreateServiceAsync(nullDto, providerId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateServiceAsync_WithNullOrWhitespaceProviderId_ShouldThrowArgumentException(
        string? invalidProviderId)
    {
        // Arrange:
        var createDto = new ServiceCreateDto
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1
        };

        // Act and Assert:
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            serviceService.CreateServiceAsync(createDto, invalidProviderId!));

        Assert.Equal("providerId", exception.ParamName);
    }

    [Fact]
    public async Task CreateServiceAsync_WithNonExistentProviderId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const string nonExistentProviderId = "non-existent-provider-user-id";
        var createDto = new ServiceCreateDto
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1
        };

        // Act and Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            serviceService.CreateServiceAsync(createDto, nonExistentProviderId));
    }

    [Fact]
    public async Task CreateServiceAsync_WithUserNotInProviderRole_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        var createDto = new ServiceCreateDto()
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1
        };

        var providerDto = new UserViewDto()
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider",
            Roles = ["Customer"]
        };

        usersServiceMock
            .Setup(s => s.GetUserByIdAsync(providerId))
            .ReturnsAsync(providerDto);

        // Act and Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            serviceService.CreateServiceAsync(createDto, providerId));
    }

    [Fact]
    public async Task CreateServiceAsync_WithNonExistentCategoryId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        const int nonExistentCategoryId = 999;
        var createDto = new ServiceCreateDto()
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = nonExistentCategoryId
        };

        var providerDto = new UserViewDto()
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider",
            Roles = ["Provider"]
        };

        usersServiceMock
            .Setup(s => s.GetUserByIdAsync(providerId))
            .ReturnsAsync(providerDto);


        categoryServiceMock
            .Setup(s => s.GetByIdAsync(createDto.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(value: null);

        // Act and Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            serviceService.CreateServiceAsync(createDto, providerId));
    }

    [Fact]
    public async Task CreateServiceAsync_WhenDatabaseSaveChangesFails_ShouldLogErrorAndThrow()
    {
        // Arrange:
        var loggerMock = new Mock<ILogger<ServiceService>>();
        await using var failingDbContext = new FailingDbContext(this.dbContextOptions);
        var serviceWithFailingDb = new ServiceService(
            failingDbContext,
            loggerMock.Object,
            this.usersServiceMock.Object,
            this.categoryServiceMock.Object
        );
        
        const string providerId = "provider-user-id";
        var createDto = new ServiceCreateDto()
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1
        };
        
        var mockProviderDto = new UserViewDto()
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider",
            Roles = ["Provider"]
        };


        var mockCategoryDto = new CategoryViewDto()
        {
            Id = createDto.CategoryId,
            Name = "Test Category"
        };
        
        
        usersServiceMock
            .Setup(s => s.GetUserByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(mockProviderDto);

        categoryServiceMock
            .Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCategoryDto);
        
        // Act & Assert:
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            serviceWithFailingDb.CreateServiceAsync(createDto, providerId));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<DbUpdateException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}