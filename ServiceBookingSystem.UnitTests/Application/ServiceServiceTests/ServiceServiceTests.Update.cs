using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    [Fact]
    public async Task UpdateServiceAsync_WithValidDataAndCorrectOwner_ShouldUpdateService()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        const int initialCategoryId = 1;
        const int updatedCategoryId = 2;
        var service = new Service()
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = initialCategoryId,
            IsOnline = true,
            ProviderId = providerId
        };
        await dbContext.AddAsync(service);
        await dbContext.SaveChangesAsync();

        var mockProviderDto = new UserViewDto
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider",
            Roles = ["Provider"]
        };
        usersServiceMock
            .Setup(s => s.GetUserByIdAsync(providerId))
            .ReturnsAsync(mockProviderDto);

        var mockCategoryDto = new CategoryViewDto { Id = updatedCategoryId, Name = "New Category" };
        categoryServiceMock
            .Setup(s => s.GetByIdAsync(updatedCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCategoryDto);

        var updateDto = new ServiceUpdateDto
        {
            Id = service.Id,
            Name = $"Updated {service.Name}",
            Description = $"Updated {service.Description}",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = updatedCategoryId,
            IsActive = true,
            IsOnline = true,
        };
        // Act:
        var resultDto = await serviceService.UpdateServiceAsync(updateDto, providerId);

        // Assert: 
        Assert.NotNull(resultDto);
        Assert.Equal(updateDto.Name, resultDto.Name);
        Assert.Equal(updateDto.Description, resultDto.Description);
        Assert.Equal(service.Price, resultDto.Price);
        Assert.Equal(service.DurationInMinutes, resultDto.DurationInMinutes);
        Assert.Equal(service.CategoryId, resultDto.CategoryId);
        Assert.Equal(service.IsOnline, resultDto.IsOnline);

        var updatedServiceInDb = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == resultDto.Id);
        Assert.NotNull(updatedServiceInDb);
        Assert.Equal(updateDto.Name, updatedServiceInDb.Name);
        Assert.Equal(updateDto.Description, updatedServiceInDb.Description);
        Assert.Equal(service.Price, updatedServiceInDb.Price);
    }

    [Fact]
    public async Task UpdateServiceAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        ServiceUpdateDto nullDto = null!;

        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => serviceService.UpdateServiceAsync(nullDto, providerId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateServiceAsync_WithNullOrWhitespaceProviderId_ShouldThrowArgumentException(
        string? invalidProviderId)
    {
        // Arrange:
        var updateDto = new ServiceUpdateDto
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1
        };

        // Act & Assert:
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            serviceService.UpdateServiceAsync(updateDto, invalidProviderId!));

        Assert.Equal("providerId", exception.ParamName);
    }

    [Fact]
    public async Task UpdateServiceAsync_WithNonExistentServiceID_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const int nonExistentId = 999;
        const string providerId = "provider-user-id";
        var updateDto = new ServiceUpdateDto
        {
            Id = nonExistentId,
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1
        };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            serviceService.UpdateServiceAsync(updateDto, providerId));
    }

    [Fact]
    public async Task UpdateServiceAsync_WhenUserIsNotOwner_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        const string attackerId = "attacker-user-id";

        var service = new Service()
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1,
            IsOnline = true,
            ProviderId = providerId
        };
        await dbContext.AddAsync(service);
        await dbContext.SaveChangesAsync();

        var updateDto = new ServiceUpdateDto
        {
            Id = service.Id,
            Name = $"Updated {service.Name}",
            Description = $"Updated {service.Description}",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1,
            IsActive = true,
            IsOnline = true,
        };
        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            serviceService.UpdateServiceAsync(updateDto, attackerId));
    }

    [Fact]
    public async Task UpdateServiceAsync_WithNonExistentCategoryId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const string providerId = "provider-user-id";
        const int invalidCategoryId = 999;
        var service = new Service()
        {
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 1,
            IsOnline = true,
            ProviderId = providerId
        };
        await dbContext.AddAsync(service);
        await dbContext.SaveChangesAsync();

        categoryServiceMock
            .Setup(s => s.GetByIdAsync(invalidCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(value: null);

        var updateDto = new ServiceUpdateDto
        {
            Id = service.Id,
            Name = "Test Service",
            Description = "A service for testing.",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = invalidCategoryId,
            IsActive = true,
            IsOnline = true,
        };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            serviceService.UpdateServiceAsync(updateDto, providerId));
    }

    [Fact]
    public async Task UpdateServiceAsync_WhenDatabaseSaveChangesFails_ShouldLogErrorAndThrow()
    {
        // Arrange:
        var loggerMock = new Mock<ILogger<ServiceService>>();

        const string providerId = "provider-user-id";
        const int serviceId = 1;
        
        await using (var setupContext = new ApplicationDbContext(dbContextOptions))
        {
            var existingService = new Service()
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "A service for testing.",
                Price = 100,
                DurationInMinutes = 60,
                CategoryId = 1,
                IsOnline = true,
                ProviderId = providerId
            };
            await setupContext.Services.AddAsync(existingService);
            await setupContext.SaveChangesAsync();
        }

        await using var failingDbContext = new FailingDbContext(dbContextOptions);


        var serviceWithFailingDb = new ServiceService(
            failingDbContext,
            loggerMock.Object,
            this.usersServiceMock.Object,
            this.categoryServiceMock.Object
        );

        var mockCategoryDto = new CategoryViewDto()
        {
            Id = 2,
            Name = "New Category"
        };

        categoryServiceMock
            .Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCategoryDto);

        var updateDto = new ServiceUpdateDto()
        {
            Id = serviceId,
            Name = "Updated Name",
            Description = "Updated description",
            Price = 100,
            DurationInMinutes = 60,
            CategoryId = 2,
            IsOnline = true,
        };

        // Act & Assert:
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            serviceWithFailingDb.UpdateServiceAsync(updateDto, providerId));

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