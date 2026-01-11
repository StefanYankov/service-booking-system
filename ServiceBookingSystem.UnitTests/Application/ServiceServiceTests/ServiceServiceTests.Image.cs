using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceBookingSystem.Application.DTOs.Image;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    [Fact]
    public async Task AddImageAsync_WithValidData_ShouldUploadAndSaveToDb()
    {
        // Arrange:
        const int serviceId = 1;
        const string providerId = "provider-1";
        const string imageUrl = "https://cloud.com/img.jpg";
        const string publicId = "img_123";

        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            Description = "Desc",
            ProviderId = providerId
        };
        await dbContext.Services.AddAsync(service);
        await dbContext.SaveChangesAsync();

        var fileMock = new Mock<IFormFile>();
        var uploadResult = new ImageStorageResult { Url = imageUrl, PublicId = publicId };

        imageServiceMock.Setup(x => x.AddImageAsync(fileMock.Object)).ReturnsAsync(uploadResult);

        // Act:
        var resultUrl = await serviceService.AddImageAsync(serviceId, providerId, fileMock.Object);

        // Assert:
        Assert.Equal(imageUrl, resultUrl);
        
        var savedImage = await dbContext.ServiceImages.FirstOrDefaultAsync(i => i.ServiceId == serviceId);
        Assert.NotNull(savedImage);
        Assert.Equal(imageUrl, savedImage.ImageUrl);
        Assert.Equal(publicId, savedImage.PublicId);
    }

    [Fact]
    public async Task AddImageAsync_WhenUserNotOwner_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const int serviceId = 1;
        const string providerId = "provider-1";
        const string otherUserId = "other-1";

        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            Description = "Desc",
            ProviderId = providerId
        };
        await dbContext.Services.AddAsync(service);
        await dbContext.SaveChangesAsync();

        var fileMock = new Mock<IFormFile>();

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            serviceService.AddImageAsync(serviceId, otherUserId, fileMock.Object));
    }

    [Fact]
    public async Task DeleteImageAsync_WithValidData_ShouldDeleteFromCloudAndDb()
    {
        // Arrange:
        const int serviceId = 1;
        const string providerId = "provider-1";
        const int imageId = 10;
        const string publicId = "img_123";

        var service = new Service
        {
            Id = serviceId, Name = "Test",
            Description = "Desc",
            ProviderId = providerId
        };
        
        var image = new ServiceImage
        {
            Id = imageId,
            ServiceId = serviceId,
            Service = service,
            ImageUrl = "url",
            PublicId = publicId
        };
        
        await dbContext.Services.AddAsync(service);
        await dbContext.ServiceImages.AddAsync(image);
        await dbContext.SaveChangesAsync();

        // Act:
        await serviceService.DeleteImageAsync(serviceId, providerId, imageId);

        // Assert:
        var deletedImage = await dbContext.ServiceImages.FindAsync(imageId);
        Assert.Null(deletedImage);
        
        imageServiceMock.Verify(x => x.DeleteImageAsync(publicId), Times.Once);
    }

    [Fact]
    public async Task DeleteImageAsync_WhenUserNotOwner_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const int serviceId = 1;
        const string providerId = "provider-1";
        const string otherUserId = "other-1";
        const int imageId = 10;

        var service = new Service
        {
            Id = serviceId,
            Name = "Test",
            Description = "Desc",
            ProviderId = providerId
        };
        
        var image = new ServiceImage
        {
            Id = imageId,
            ServiceId = serviceId,
            Service = service,
            ImageUrl = "url"
        };
        
        await dbContext.Services.AddAsync(service);
        await dbContext.ServiceImages.AddAsync(image);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            serviceService.DeleteImageAsync(serviceId, otherUserId, imageId));
    }

    [Fact]
    public async Task DeleteImageAsync_WhenImageNotFound_ShouldThrowEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            serviceService.DeleteImageAsync(1, "user", 999));
    }
}