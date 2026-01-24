using Moq;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.BookingServiceTests;

public partial class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_ShouldSendRealTimeNotificationToProvider()
    {
        // Arrange
        const string customerId = "customer-1";
        const string providerId = "provider-1";
        const int serviceId = 1;
        var bookingStart = DateTime.UtcNow.AddDays(1);

        var provider = new ApplicationUser { Id = providerId, UserName = "p", Email = "p@test.com", FirstName = "P", LastName = "T" };
        var customer = new ApplicationUser { Id = customerId, UserName = "c", Email = "c@test.com", FirstName = "C", LastName = "T" };
        var category = new Category { Id = 1, Name = "Cat" };
        var service = new Service 
        { 
            Id = serviceId, 
            Name = "Test Service", 
            Description = "Desc", 
            ProviderId = providerId, 
            CategoryId = 1,
            IsActive = true,
            DurationInMinutes = 60
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);
        await dbContext.SaveChangesAsync();

        var dto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = bookingStart,
            Notes = "Test"
        };

        var serviceDto = new ServiceViewDto
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Desc",
            ProviderId = providerId,
            ProviderName = "P T",
            CategoryName = "Cat",
            IsActive = true,
            DurationInMinutes = 60
        };

        var customerDto = new UserViewDto
        {
            Id = customerId,
            FirstName = "C",
            LastName = "T",
            Email = "c@test.com"
        };

        serviceServiceMock.Setup(s => s.GetServiceByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceDto);
        
        availabilityServiceMock.Setup(a => a.IsSlotAvailableAsync(serviceId, bookingStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        usersServiceMock.Setup(u => u.GetUserByIdAsync(customerId))
            .ReturnsAsync(customerDto);

        // Act
        await bookingService.CreateBookingAsync(dto, customerId);

        // Assert
        realTimeNotificationServiceMock.Verify(
            x => x.SendToUserAsync(providerId, It.Is<string>(s => s.Contains("New booking request")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateBookingAsync_WhenRescheduled_ShouldSendRealTimeNotificationToProvider()
    {
        // Arrange
        var customerId = "customer-1";
        var providerId = "provider-1";
        var oldStart = DateTime.UtcNow.AddDays(1);
        var newStart = DateTime.UtcNow.AddDays(2);

        var provider = new ApplicationUser { Id = providerId, UserName = "p", Email = "p@test.com", FirstName = "P", LastName = "T" };
        var customer = new ApplicationUser { Id = customerId, UserName = "c", Email = "c@test.com", FirstName = "C", LastName = "T" };
        var category = new Category { Id = 1, Name = "Cat" };
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Desc",
            ProviderId = providerId,
            CategoryId = 1,
            DurationInMinutes = 60
        };
        
        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);
        
        var booking = new Booking
        {
            ServiceId = 1,
            CustomerId = customerId,
            BookingStart = oldStart,
            Status = BookingStatus.Pending
        };
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var updateDto = new BookingUpdateDto
        {
            Id = booking.Id, // Use the generated ID
            BookingStart = newStart,
            Notes = "Rescheduled"
        };

        availabilityServiceMock.Setup(a => a.IsSlotAvailableAsync(1, newStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await bookingService.UpdateBookingAsync(updateDto, customerId);

        // Assert
        realTimeNotificationServiceMock.Verify(
            x => x.SendToUserAsync(providerId, It.Is<string>(s => s.Contains("Booking rescheduled")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmBookingAsync_ShouldSendRealTimeNotificationToCustomer()
    {
        // Arrange
        var customerId = "customer-1";
        var providerId = "provider-1";
        
        var provider = new ApplicationUser { Id = providerId, UserName = "p", Email = "p@test.com", FirstName = "P", LastName = "T" };
        var customer = new ApplicationUser { Id = customerId, UserName = "c", Email = "c@test.com", FirstName = "C", LastName = "T" };
        var category = new Category { Id = 1, Name = "Cat" };
        var service = new Service { Id = 1, Name = "Test Service", Description = "D", ProviderId = providerId, CategoryId = 1 };
        
        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);

        var booking = new Booking
        {
            ServiceId = 1,
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending
        };
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act
        await bookingService.ConfirmBookingAsync(booking.Id, providerId);

        // Assert
        realTimeNotificationServiceMock.Verify(
            x => x.SendToUserAsync(customerId, It.Is<string>(s => s.Contains("confirmed")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeclineBookingAsync_ShouldSendRealTimeNotificationToCustomer()
    {
        // Arrange
        var customerId = "customer-1";
        var providerId = "provider-1";
        
        var provider = new ApplicationUser { Id = providerId, UserName = "p", Email = "p@test.com", FirstName = "P", LastName = "T" };
        var customer = new ApplicationUser { Id = customerId, UserName = "c", Email = "c@test.com", FirstName = "C", LastName = "T" };
        var category = new Category { Id = 1, Name = "Cat" };
        var service = new Service { Id = 1, Name = "Test Service", Description = "D", ProviderId = providerId, CategoryId = 1 };
        
        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);

        var booking = new Booking
        {
            ServiceId = 1,
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending
        };
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act
        await bookingService.DeclineBookingAsync(booking.Id, providerId);

        // Assert
        realTimeNotificationServiceMock.Verify(
            x => x.SendToUserAsync(customerId, It.Is<string>(s => s.Contains("declined")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_ByProvider_ShouldSendRealTimeNotificationToCustomer()
    {
        // Arrange
        var customerId = "customer-1";
        var providerId = "provider-1";
        
        var provider = new ApplicationUser { Id = providerId, UserName = "p", Email = "p@test.com", FirstName = "P", LastName = "T" };
        var customer = new ApplicationUser { Id = customerId, UserName = "c", Email = "c@test.com", FirstName = "C", LastName = "T" };
        var category = new Category { Id = 1, Name = "Cat" };
        var service = new Service { Id = 1, Name = "Test Service", Description = "D", ProviderId = providerId, CategoryId = 1 };
        
        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);

        var booking = new Booking
        {
            ServiceId = 1,
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Confirmed
        };
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act
        await bookingService.CancelBookingAsync(booking.Id, providerId);

        // Assert
        realTimeNotificationServiceMock.Verify(
            x => x.SendToUserAsync(customerId, It.Is<string>(s => s.Contains("cancelled by the provider")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_ByCustomer_ShouldSendRealTimeNotificationToProvider()
    {
        // Arrange
        var customerId = "customer-1";
        var providerId = "provider-1";
        
        var provider = new ApplicationUser { Id = providerId, UserName = "p", Email = "p@test.com", FirstName = "P", LastName = "T" };
        var customer = new ApplicationUser { Id = customerId, UserName = "c", Email = "c@test.com", FirstName = "C", LastName = "T" };
        var category = new Category { Id = 1, Name = "Cat" };
        var service = new Service { Id = 1, Name = "Test Service", Description = "D", ProviderId = providerId, CategoryId = 1 };
        
        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);

        var booking = new Booking
        {
            ServiceId = 1,
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Confirmed
        };
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act
        await bookingService.CancelBookingAsync(booking.Id, customerId);

        // Assert
        realTimeNotificationServiceMock.Verify(
            x => x.SendToUserAsync(providerId, It.Is<string>(s => s.Contains("cancelled by the customer")), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}