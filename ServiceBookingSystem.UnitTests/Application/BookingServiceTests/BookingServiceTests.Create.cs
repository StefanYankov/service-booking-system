using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.UnitTests.Application.BookingServiceTests;

public partial class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_WithValidData_ShouldCreateBookingAndReturnDto()
    {
        // Arrange:
        const string customerId = "customer-1";
        const string providerId = "provider-1";
        const int serviceId = 1;
        var bookingStart = DateTime.UtcNow.AddDays(1);
        var dto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = bookingStart, Notes = "Test Note"
        };

        var serviceDto = new ServiceViewDto 
        { 
            Id = serviceId, 
            Name = "Test Service", 
            Description = "Test Description",
            CategoryName = "Test Category",
            IsActive = true, 
            ProviderId = providerId, 
            ProviderName = "Test Provider",
            DurationInMinutes = 60 
        };
        
        var customerDto = new UserViewDto { Id = customerId, FirstName = "Test", LastName = "Customer" };

        serviceServiceMock
            .Setup(x => x.GetServiceByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceDto);
            
        usersServiceMock
            .Setup(x => x.GetUserByIdAsync(customerId))
            .ReturnsAsync(customerDto);
            
        availabilityServiceMock
            .Setup(x => x.IsSlotAvailableAsync(serviceId, bookingStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act:
        var result = await bookingService.CreateBookingAsync(dto, customerId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("Test Note", result.Notes);
        Assert.Equal(bookingStart, result.BookingStart);
        
        // Verify DB persistence
        var savedBooking = await dbContext.Bookings.FirstOrDefaultAsync();
        Assert.NotNull(savedBooking);
        Assert.Equal(serviceId, savedBooking.ServiceId);
        Assert.Equal(customerId, savedBooking.CustomerId);
        Assert.Equal(bookingStart, savedBooking.BookingStart);
        Assert.Equal("Test Note", savedBooking.Notes);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            bookingService.CreateBookingAsync(null!, "customer-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateBookingAsync_WithInvalidCustomerId_ShouldThrowArgumentException(string? customerId)
    {
        // Arrange:
        var dto = new BookingCreateDto { ServiceId = 1, BookingStart = DateTime.UtcNow.AddDays(1) };

        // Act: & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            bookingService.CreateBookingAsync(dto, customerId!));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenServiceNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var dto = new BookingCreateDto
        {
            ServiceId = 999,
            BookingStart = DateTime.UtcNow.AddDays(1)
        };
        
        serviceServiceMock
            .Setup(x => x.GetServiceByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceViewDto?)null);

        // Act: & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            bookingService.CreateBookingAsync(dto, "customer-1"));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenServiceNotActive_ShouldThrowServiceNotActiveException()
    {
        // Arrange:
        const int serviceId = 1;
        var dto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = DateTime.UtcNow.AddDays(1)
        };
        var serviceDto = new ServiceViewDto 
        { 
            Id = serviceId, 
            IsActive = false, 
            Name = "Inactive Service",
            Description = "Test Description",
            CategoryName = "Test Category",
            ProviderName = "Test Provider",
            ProviderId = "provider-1"
        };

        serviceServiceMock
            .Setup(x => x.GetServiceByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceDto);

        // Act: & Assert
        var ex = await Assert.ThrowsAsync<ServiceNotActiveException>(() => 
            bookingService.CreateBookingAsync(dto, "customer-1"));
        Assert.Equal(serviceId, ex.ServiceId);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenProviderBooksOwnService_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string providerId = "provider-1";
        const int serviceId = 1;
        var dto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = DateTime.UtcNow.AddDays(1)
        };
        var serviceDto = new ServiceViewDto 
        { 
            Id = serviceId, 
            IsActive = true, 
            ProviderId = providerId,
            Name = "Test Service",
            Description = "Test Description",
            CategoryName = "Test Category",
            ProviderName = "Test Provider"
        };

        serviceServiceMock
            .Setup(x => x.GetServiceByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceDto);

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            bookingService.CreateBookingAsync(dto, providerId)); // Customer IS Provider
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSlotUnavailable_ShouldThrowSlotUnavailableException()
    {
        // Arrange:
        const int serviceId = 1;
        var bookingStart = DateTime.UtcNow.AddDays(1);
        var dto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = bookingStart
        };
        
        var serviceDto = new ServiceViewDto 
        { 
            Id = serviceId, 
            IsActive = true, 
            ProviderId = "provider-1", 
            DurationInMinutes = 60,
            Name = "Test Service",
            Description = "Test Description",
            CategoryName = "Test Category",
            ProviderName = "Test Provider"
        };

        serviceServiceMock
            .Setup(x => x.GetServiceByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceDto);
            
        availabilityServiceMock
            .Setup(x => x.IsSlotAvailableAsync(serviceId, bookingStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Slot taken

        // Act & Assert:
        var ex = await Assert.ThrowsAsync<SlotUnavailableException>(() => 
            bookingService.CreateBookingAsync(dto, "customer-1"));
        Assert.Equal(serviceId, ex.ServiceId);
        Assert.Equal(bookingStart, ex.SlotStart);
    }
    
    [Fact]
    public async Task CreateBookingAsync_WhenCustomerNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        const string customerId = "customer-1";
        const int serviceId = 1;
        var bookingStart = DateTime.UtcNow.AddDays(1);
        var dto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = bookingStart
        };
        
        var serviceDto = new ServiceViewDto 
        { 
            Id = serviceId, 
            IsActive = true, 
            ProviderId = "provider-1", 
            DurationInMinutes = 60,
            Name = "Test Service",
            Description = "Test Description",
            CategoryName = "Test Category",
            ProviderName = "Test Provider"
        };

        serviceServiceMock
            .Setup(x => x.GetServiceByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceDto);
            
        availabilityServiceMock
            .Setup(x => x.IsSlotAvailableAsync(serviceId, bookingStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        usersServiceMock
            .Setup(x => x.GetUserByIdAsync(customerId))
            .ReturnsAsync((UserViewDto?)null); // Customer not found

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            bookingService.CreateBookingAsync(dto, customerId));
    }
}