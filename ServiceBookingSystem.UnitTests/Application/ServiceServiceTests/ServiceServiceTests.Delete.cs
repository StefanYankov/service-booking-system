using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.UnitTests.Application.ServiceServiceTests;

public partial class ServiceServiceTests
{
    [Fact]
    public async Task DeleteServiceAsync_WithValidIdAndCorrectOwner_ShouldSoftDeleteService()
    {
        // Arrange:
        const string providerId = "provider-user-id";

        var service = new Service
        {
            Name = "Service to Delete",
            Description = "Test Description",
            ProviderId = providerId,
            CategoryId = 1
        };
        await dbContext.AddAsync(service);
        await dbContext.SaveChangesAsync();

        // Act:
        await this.serviceService.DeleteServiceAsync(service.Id, providerId);

        // Assert:
        this.dbContext.ChangeTracker.Clear(); // clear in-memory db cache

        var serviceWithFilter = await this.dbContext.Services.FindAsync(service.Id);
        Assert.Null(serviceWithFilter);

        var deletedService = await this.dbContext.Services
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == service.Id);

        Assert.NotNull(deletedService);
        Assert.True(deletedService.IsDeleted);
        Assert.NotNull(deletedService.DeletedOn);
    }

    [Fact]
    public async Task DeleteServiceAsync_WhenUserIsNotOwner_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string ownerId = "owner-id";
        const string attackerId = "attacker-id";

        var service = new Service
        {
            Name = "Protected Service",
            Description = "Test Description",
            ProviderId = ownerId,
            CategoryId = 1,
            DurationInMinutes = 60
        };
        await this.dbContext.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            this.serviceService.DeleteServiceAsync(service.Id, attackerId));

        var serviceInDb = await this.dbContext.Services.FindAsync(service.Id);
        Assert.NotNull(serviceInDb);
        Assert.False(serviceInDb.IsDeleted);
    }

    [Fact]
    public async Task DeleteServiceAsync_WithNonExistentServiceId_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const string providerId = "provider-id";
        const int nonExistentId = 999;

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            this.serviceService.DeleteServiceAsync(nonExistentId, providerId));
    }

    [Fact]
    public async Task DeleteServiceAsync_WhenServiceIsSoftDeleted_ShouldHideRelatedEntitiesViaQueryFilters()
    {
        // Arrange:
        const string providerId = "provider-id";

        var service = new Service
        {
            Name = "Parent Service",
            Description = "Test",
            ProviderId = providerId,
            CategoryId = 1,
            DurationInMinutes = 60
        };
        await this.dbContext.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        var review = new Review
        {
            ServiceId = service.Id,
            CustomerId = "customer-1",
            Rating = 5,
            BookingId = "dummy-booking-id"
        };

        var operatingHour = new OperatingHour
        {
            ServiceId = service.Id,
            DayOfWeek = DayOfWeek.Monday,
            Segments = new List<OperatingSegment>
            {
                new() { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) }
            }
        };
        var image = new ServiceImage
        {
            ServiceId = service.Id,
            ImageUrl = "http://test.com"
        };

        await this.dbContext.AddRangeAsync(review, operatingHour, image);
        await this.dbContext.SaveChangesAsync();

        // Act:
        await this.serviceService.DeleteServiceAsync(service.Id, providerId);

        // Assert:
        var reviewsCount = await this.dbContext.Reviews.CountAsync(r => r.ServiceId == service.Id);
        var hoursCount = await this.dbContext.OperatingHours.CountAsync(oh => oh.ServiceId == service.Id);
        var imagesCount = await this.dbContext.ServiceImages.CountAsync(si => si.ServiceId == service.Id);

        Assert.Equal(0, reviewsCount);
        Assert.Equal(0, hoursCount);
        Assert.Equal(0, imagesCount);
    }

    [Fact]
    public async Task DeleteServiceAsync_WhenServiceIsSoftDeleted_RelatedEntitiesShouldNotBePhysicallyAltered()
    {
        // Arrange:
        const string providerId = "provider-id";

        var service = new Service
        {
            Name = "Parent Service",
            Description = "Test",
            ProviderId = providerId,
            CategoryId = 1,
            DurationInMinutes = 60
        };
        await this.dbContext.AddAsync(service);
        await this.dbContext.SaveChangesAsync();

        var review = new Review
        {
            ServiceId = service.Id,
            CustomerId = "customer-1",
            Rating = 5,
            BookingId = "dummy-booking-id"
        };
        var operatingHour = new OperatingHour
        {
            ServiceId = service.Id,
            DayOfWeek = DayOfWeek.Tuesday,
            Segments = new List<OperatingSegment>
            {
                new() { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) }
            }
        };
        var image = new ServiceImage
        {
            ServiceId = service.Id,
            ImageUrl = "http://test.com/image.jpg"
        };

        await this.dbContext.AddRangeAsync(review, operatingHour, image);
        await this.dbContext.SaveChangesAsync();

        // Act:
        await this.serviceService.DeleteServiceAsync(service.Id, providerId);

        // Assert:
        var hiddenReview =
            await this.dbContext.Reviews.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == review.Id);
        var hiddenHour = await this.dbContext.OperatingHours.IgnoreQueryFilters()
            .FirstOrDefaultAsync(oh => oh.Id == operatingHour.Id);
        var hiddenImage = await this.dbContext.ServiceImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(si => si.Id == image.Id);

        Assert.NotNull(hiddenReview);
        Assert.False(hiddenReview.IsDeleted);

        Assert.NotNull(hiddenHour);
        Assert.NotNull(hiddenImage);
    }

    [Fact]
    public async Task DeleteServiceAsync_WhenDatabaseSaveChangesFails_ShouldThrowAndNotAlterState()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ServiceService>>();
        const string providerId = "provider-id";

        int serviceId;
        await using (var setupContext = new ApplicationDbContext(this.dbContextOptions))
        {
            var serviceToSeed = new Service
            {
                Name = "Service",
                Description = "Test",
                ProviderId = providerId,
                CategoryId = 1,
                DurationInMinutes = 60
            };
            await setupContext.Services.AddAsync(serviceToSeed);
            await setupContext.SaveChangesAsync();
            serviceId = serviceToSeed.Id;
        }

        await using var failingTestContext = new FailingDbContext(this.dbContextOptions);

        var serviceWithFailingDb = new ServiceService(
            failingTestContext,
            loggerMock.Object,
            this.usersServiceMock.Object,
            this.categoryServiceMock.Object,
            this.imageServiceMock.Object);

        // Act & Assert:
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            serviceWithFailingDb.DeleteServiceAsync(serviceId, providerId));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<DbUpdateException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        var serviceInDb = await this.dbContext.Services.FindAsync(serviceId);
        Assert.NotNull(serviceInDb);
        Assert.False(serviceInDb.IsDeleted);
    }
}