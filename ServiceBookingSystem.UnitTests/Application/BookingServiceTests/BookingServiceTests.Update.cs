using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.BookingServiceTests;

public partial class BookingServiceTests
{
    [Fact]
    public async Task UpdateBookingAsync_WithValidData_ShouldUpdateBookingAndReturnDto()
    {
        // Arrange::
        const string customerId = "customer-1";
        const string bookingId = "booking-1";
        const int serviceId = 1;
        var originalStart = DateTime.UtcNow.AddDays(1);
        
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
        };
        var customer = new ApplicationUser
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "Customer"
        };
        var service = new Service { 
            Id = serviceId,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider, DurationInMinutes = 60 
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = serviceId, 
            Service = service, 
            CustomerId = customerId, 
            Customer = customer,
            BookingStart = originalStart, 
            Status = BookingStatus.Pending,
            Notes = "Old Note"
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var dto = new BookingUpdateDto 
        { 
            Id = bookingId, 
            BookingStart = originalStart, // Same time
            Notes = "New Note" 
        };

        // Act:
        var result = await bookingService.UpdateBookingAsync(dto, customerId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal("New Note", result.Notes);
        Assert.Equal(originalStart, result.BookingStart);
        Assert.Equal("Pending", result.Status); // Status should remain Pending

        // Verify DB
        var updatedBooking = await dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal("New Note", updatedBooking!.Notes);
    }

    [Fact]
    public async Task UpdateBookingAsync_WithReschedule_ShouldUpdateBookingAndResetStatus()
    {
        // Arrange:
        var customerId = "customer-1";
        var bookingId = "booking-1";
        var serviceId = 1;
        var originalStart = DateTime.UtcNow.AddDays(1);
        var newStart = DateTime.UtcNow.AddDays(2);
        
        var provider = new ApplicationUser { Id = "provider-1", FirstName = "Test", LastName = "Provider" };
        var customer = new ApplicationUser { Id = customerId, FirstName = "Test", LastName = "Customer" };
        var service = new Service { Id = serviceId, Name = "Test Service", Description = "Test Description", ProviderId = "provider-1", Provider = provider, DurationInMinutes = 60 };
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = serviceId, 
            Service = service, 
            CustomerId = customerId, 
            Customer = customer,
            BookingStart = originalStart, 
            Status = BookingStatus.Confirmed, // Was Confirmed
            Notes = "Old Note"
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var dto = new BookingUpdateDto 
        { 
            Id = bookingId, 
            BookingStart = newStart, // New time
            Notes = "New Note" 
        };

        availabilityServiceMock.Setup(x => x.IsSlotAvailableAsync(serviceId, newStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act:
        var result = await bookingService.UpdateBookingAsync(dto, customerId);

        // Assert:
        Assert.Equal(newStart, result.BookingStart);
        Assert.Equal("Pending", result.Status); // Should reset to Pending

        // Verify DB
        var updatedBooking = await dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Pending, updatedBooking!.Status);
    }

    [Fact]
    public async Task UpdateBookingAsync_WhenBookingNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var dto = new BookingUpdateDto { Id = "non-existent", BookingStart = DateTime.UtcNow };

        // Act: & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            bookingService.UpdateBookingAsync(dto, "customer-1"));
    }

    [Fact]
    public async Task UpdateBookingAsync_WhenUserNotOwner_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string bookingId = "booking-1";
        const string ownerId = "owner-1";
        const string otherUserId = "other-1";
        
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "Test", 
            LastName = "Provider"
        };
        var owner = new ApplicationUser
        {
            Id = ownerId,
            FirstName = "Owner",
            LastName = "User"
        };
        var service = new Service
        {
            Id = 1, Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = ownerId, 
            Customer = owner,
            BookingStart = DateTime.UtcNow.AddDays(1) 
        };

        await dbContext.Users.AddRangeAsync(provider, owner);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var dto = new BookingUpdateDto { Id = bookingId, BookingStart = DateTime.UtcNow.AddDays(1) };

        // Act: & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            bookingService.UpdateBookingAsync(dto, otherUserId));
    }

    [Fact]
    public async Task UpdateBookingAsync_WhenBookingCancelled_ShouldThrowInvalidBookingStateException()
    {
        // Arrange:
        const string bookingId = "booking-1";
        var customerId = "customer-1";
        
        var provider = new ApplicationUser 
        {
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
        };
        var customer = new ApplicationUser 
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "Customer"
        };
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Description", 
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = customerId, 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Cancelled // Invalid state for update
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var dto = new BookingUpdateDto { Id = bookingId, BookingStart = DateTime.UtcNow.AddDays(1) };

        // Act: & Assert:
        var ex = await Assert.ThrowsAsync<InvalidBookingStateException>(() => 
            bookingService.UpdateBookingAsync(dto, customerId));
        Assert.Equal("Cancelled", ex.CurrentState);
        Assert.Equal("Update", ex.Action);
    }

    [Fact]
    public async Task UpdateBookingAsync_WhenSlotUnavailable_ShouldThrowSlotUnavailableException()
    {
        // Arrange:
        const string bookingId = "booking-1";
        const string customerId = "customer-1";
        const int serviceId = 1;
        var newStart = DateTime.UtcNow.AddDays(2);
        
        var provider = new ApplicationUser 
        {
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "Customer"
        };
        var service = new Service
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider, DurationInMinutes = 60
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = serviceId, 
            Service = service, 
            CustomerId = customerId, 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var dto = new BookingUpdateDto { Id = bookingId, BookingStart = newStart };

        availabilityServiceMock.Setup(x => x.IsSlotAvailableAsync(serviceId, newStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Slot unavailable

        // Act: & Assert:
        await Assert.ThrowsAsync<SlotUnavailableException>(() => 
            bookingService.UpdateBookingAsync(dto, customerId));
    }

    [Fact]
    public async Task UpdateBookingAsync_WithSameTime_ShouldNotCallAvailabilityService()
    {
        // Arrange:
        const string customerId = "customer-1";
        const string bookingId = "booking-1";
        var originalStart = DateTime.UtcNow.AddDays(1);
        
        var provider = new ApplicationUser 
        {
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "Customer"
        };
        
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = customerId, 
            Customer = customer,
            BookingStart = originalStart, 
            Status = BookingStatus.Pending
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var dto = new BookingUpdateDto { Id = bookingId, BookingStart = originalStart }; // Same time

        // Act::
        await bookingService.UpdateBookingAsync(dto, customerId);

        // Assert::
        availabilityServiceMock.Verify(x => x.IsSlotAvailableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "customer-1")] // Null DTO
    [InlineData("valid-dto", "")] // Empty CustomerId
    [InlineData("valid-dto", "   ")] // Whitespace CustomerId
    [InlineData("valid-dto", null)] // Null CustomerId
    public async Task UpdateBookingAsync_WithInvalidInput_ShouldThrowArgumentException(string? dtoState, string? customerId)
    {
        // Arrange::
        BookingUpdateDto? dto = null;
        if (dtoState == "valid-dto")
        {
            dto = new BookingUpdateDto 
            { 
                Id = "booking-1", 
                BookingStart = DateTime.UtcNow.AddDays(1) 
            };
        }

        // Act: & Assert:
        await Assert.ThrowsAnyAsync<ArgumentException>(() => 
            bookingService.UpdateBookingAsync(dto!, customerId));
    }

    [Fact]
    public async Task UpdateBookingAsync_Reschedule_ShouldNotifyProvider()
    {
        // Arrange:
        const string providerId = "provider-1";
        const string customerId = "customer-1";
        var booking = await SeedBookingForUpdateAsync(providerId, customerId);
        var oldStart = booking.BookingStart;
        var newStart = oldStart.AddDays(1);

        var dto = new BookingUpdateDto
        {
            Id = booking.Id,
            BookingStart = newStart,
            Notes = "Rescheduling"
        };

        availabilityServiceMock
            .Setup(x => x.IsSlotAvailableAsync(booking.ServiceId, newStart, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act:
        var result = await bookingService.UpdateBookingAsync(dto, customerId);

        // Assert:
        Assert.Equal(newStart, result.BookingStart);
        
        // Verify Notification
        notificationServiceMock
            .Verify(x => x.NotifyBookingRescheduledAsync(It.Is<Booking>(b => b.Id == booking.Id), oldStart), Times.Once);
    }

    [Fact]
    public async Task UpdateBookingAsync_NotesOnly_ShouldNotNotify()
    {
        // Arrange:
        var providerId = "provider-1";
        var customerId = "customer-1";
        var booking = await SeedBookingForUpdateAsync(providerId, customerId);
        var oldStart = booking.BookingStart;

        var dto = new BookingUpdateDto
        {
            Id = booking.Id,
            BookingStart = oldStart, // Same date
            Notes = "Just updating notes"
        };

        // Act:
        var result = await bookingService.UpdateBookingAsync(dto, customerId);

        // Assert:
        Assert.Equal("Just updating notes", result.Notes);
        
        notificationServiceMock
            .Verify(x => x.NotifyBookingRescheduledAsync(It.IsAny<Booking>(), It.IsAny<DateTime>()), Times.Never);
    }

    private async Task<Booking> SeedBookingForUpdateAsync(string providerId, string customerId)
    {
        var provider = new ApplicationUser 
        {
            Id = providerId,
            FirstName = "Provider",
            LastName = "Provider",
            Email = "prov@test.com"
        };
        
        var customer = new ApplicationUser 
        {
            Id = customerId,
            FirstName = "Customer",
            LastName = "Customer",
            Email = "cust@test.com"
        };
        
        var service = new Service 
        { 
            Id = 1, 
            Name = "Test Service", 
            Description = "Desc", 
            ProviderId = providerId, 
            Provider = provider,
            DurationInMinutes = 60 
        };

        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            ServiceId = 1,
            Service = service,
            CustomerId = customerId,
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Confirmed
        };

        if (!await dbContext.Users.AnyAsync(u => u.Id == providerId))
        {
            await dbContext.Users.AddAsync(provider);
        }

        if (!await dbContext.Users.AnyAsync(u => u.Id == customerId))
        {
            await dbContext.Users.AddAsync(customer);
        }

        if (!await dbContext.Services.AnyAsync(s => s.Id == 1))
        {
            await dbContext.Services.AddAsync(service);
        }

        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        return booking;
    }
}