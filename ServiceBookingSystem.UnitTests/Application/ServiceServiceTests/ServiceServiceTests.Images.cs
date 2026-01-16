using Microsoft.AspNetCore.Http;
using Moq;
using ServiceBookingSystem.Application.DTOs.Image;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    [Fact]
    public async Task AddImageAsync_WithValidData_ShouldAddImageAndReturnUrl()
    {
        // Arrange
        const string providerId = "provider-1";
        const int serviceId = 1;
        const string imageUrl = "https://example.com/image.jpg";
        const string publicId = "public-id";

        var provider = new ApplicationUser { Id = providerId, FirstName = "John", LastName = "Doe" };
        var category = new Category { Id = 1, Name = "Test Category" };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Description",
            ProviderId = providerId,
            CategoryId = 1
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        var fileMock = new Mock<IFormFile>();
        this.imageServiceMock.Setup(x => x.AddImageAsync(fileMock.Object))
            .ReturnsAsync(new ImageStorageResult { Url = imageUrl, PublicId = publicId });

        // Act
        var result = await this.serviceService.AddImageAsync(serviceId, providerId, fileMock.Object);

        // Assert
        Assert.Equal(imageUrl, result);
        
        var imageInDb = this.dbContext.ServiceImages.FirstOrDefault(i => i.ServiceId == serviceId);
        Assert.NotNull(imageInDb);
        Assert.Equal(imageUrl, imageInDb.ImageUrl);
        Assert.Equal(publicId, imageInDb.PublicId);
    }

    [Fact]
    public async Task AddImageAsync_WhenServiceNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            this.serviceService.AddImageAsync(999, "provider-1", fileMock.Object));
    }

    [Fact]
    public async Task AddImageAsync_WhenUserIsNotOwner_ShouldThrowAuthorizationException()
    {
        // Arrange
        const string providerId = "provider-1";
        const int serviceId = 1;

        var provider = new ApplicationUser { Id = providerId, FirstName = "John", LastName = "Doe" };
        var category = new Category { Id = 1, Name = "Test Category" };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Description",
            ProviderId = providerId,
            CategoryId = 1
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        var fileMock = new Mock<IFormFile>();

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            this.serviceService.AddImageAsync(serviceId, "other-user", fileMock.Object));
    }

    [Fact]
    public async Task DeleteImageAsync_WithValidData_ShouldDeleteImage()
    {
        // Arrange
        const string providerId = "provider-1";
        const int serviceId = 1;
        const int imageId = 1;
        const string publicId = "public-id";

        var provider = new ApplicationUser { Id = providerId, FirstName = "John", LastName = "Doe" };
        var category = new Category { Id = 1, Name = "Test Category" };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Description",
            ProviderId = providerId,
            CategoryId = 1
        };
        var image = new ServiceImage
        {
            Id = imageId,
            ServiceId = serviceId,
            ImageUrl = "url",
            PublicId = publicId
        };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.ServiceImages.AddAsync(image);
        await this.dbContext.SaveChangesAsync();

        // Act
        await this.serviceService.DeleteImageAsync(serviceId, providerId, imageId);

        // Assert
        var imageInDb = await this.dbContext.ServiceImages.FindAsync(imageId);
        Assert.Null(imageInDb);
        
        this.imageServiceMock.Verify(x => x.DeleteImageAsync(publicId), Times.Once);
    }

    [Fact]
    public async Task SetThumbnailAsync_WithValidData_ShouldUpdateFlags()
    {
        // Arrange
        const string providerId = "provider-1";
        const int serviceId = 1;

        var provider = new ApplicationUser { Id = providerId, FirstName = "John", LastName = "Doe" };
        var category = new Category { Id = 1, Name = "Test Category" };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Description",
            ProviderId = providerId,
            CategoryId = 1
        };
        
        var image1 = new ServiceImage { Id = 1, ServiceId = serviceId, ImageUrl = "url1", IsThumbnail = true };
        var image2 = new ServiceImage { Id = 2, ServiceId = serviceId, ImageUrl = "url2", IsThumbnail = false };

        await this.dbContext.Users.AddAsync(provider);
        await this.dbContext.Categories.AddAsync(category);
        await this.dbContext.Services.AddAsync(service);
        await this.dbContext.ServiceImages.AddRangeAsync(image1, image2);
        await this.dbContext.SaveChangesAsync();

        // Act
        await this.serviceService.SetThumbnailAsync(serviceId, providerId, 2);

        // Assert
        var img1 = await this.dbContext.ServiceImages.FindAsync(1);
        var img2 = await this.dbContext.ServiceImages.FindAsync(2);
        
        Assert.False(img1!.IsThumbnail);
        Assert.True(img2!.IsThumbnail);
    }
}