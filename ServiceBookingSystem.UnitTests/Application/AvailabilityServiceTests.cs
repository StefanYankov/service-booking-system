using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application;

public class AvailabilityServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> dbContextOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly AvailabilityService availabilityService;

    public AvailabilityServiceTests()
    {
        dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"AvailabilityTests_{Guid.NewGuid()}")
            .Options;
        dbContext = new ApplicationDbContext(dbContextOptions);
        var logger = new Mock<ILogger<AvailabilityService>>().Object;
        availabilityService = new AvailabilityService(dbContext, logger);
    }

    private async Task SeedServiceAndProvider()
    {
        var provider = new ApplicationUser { 
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
            
        };
        
        var category = new Category
        {
            Id = 1,
            Name = "Test Category"
        };
        
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Desc",
            DurationInMinutes = 60,
            ProviderId = "provider-1",
            CategoryId = 1
        };

        await dbContext.Users.AddAsync(provider);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Services.AddAsync(service);
        await dbContext.SaveChangesAsync();
    }
    
    public void Dispose()
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Dispose();
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenServiceNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var futureTime = DateTime.UtcNow.AddDays(1);

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            availabilityService.IsSlotAvailableAsync(999, futureTime, 60));
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenTimeIsInThePast_ShouldReturnFalse()
    {
        // Arrange:
        await SeedServiceAndProvider();
        var pastTime = DateTime.UtcNow.AddMinutes(-1);

        // Act:
        var result = await availabilityService.IsSlotAvailableAsync(1, pastTime, 60);

        // Assert:
        Assert.False(result);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenOnDayWithNoOperatingHours_ShouldReturnFalse()
    {
        // Arrange:
        await SeedServiceAndProvider();
        // Operating hours are only on Monday
        var operatingHour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        await dbContext.OperatingHours.AddAsync(operatingHour);
        await dbContext.SaveChangesAsync();

        // Try to book on a Tuesday
        var nextTuesday = DateTime.UtcNow.AddDays(1);
        while (nextTuesday.DayOfWeek != DayOfWeek.Tuesday)
        {
            nextTuesday = nextTuesday.AddDays(1);
        }
        var bookingTime = new DateTime(nextTuesday.Year, nextTuesday.Month, nextTuesday.Day, 10, 0, 0, DateTimeKind.Utc);

        // Act:
        var result = await availabilityService.IsSlotAvailableAsync(1, bookingTime, 60);

        // Assert:
        Assert.False(result);
    }

    [Theory]
    [InlineData(8, 0)]  // Before opening
    [InlineData(16, 1)] // Ends after closing
    public async Task IsSlotAvailableAsync_WhenOutsideOperatingHours_ShouldReturnFalse(int hour, int minute)
    {
        // Arrange:
        await SeedServiceAndProvider();
        var operatingHour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        
        await dbContext.OperatingHours.AddAsync(operatingHour);
        await dbContext.SaveChangesAsync();

        var nextMonday = DateTime.UtcNow.AddDays(1);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        var bookingTime = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, hour, minute, 0, DateTimeKind.Utc);

        // Act:
        var result = await availabilityService.IsSlotAvailableAsync(1, bookingTime, 60);

        // Assert:
        Assert.False(result);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenSlotOverlapsWithExistingBooking_ShouldReturnFalse()
    {
        // Arrange:
        await SeedServiceAndProvider();
        var operatingHour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        
        await dbContext.OperatingHours.AddAsync(operatingHour);

        var nextMonday = DateTime.UtcNow.AddDays(1);
        
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        
        var existingBookingStart = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 10, 0, 0, DateTimeKind.Utc);
        var existingBooking = new Booking
        {
            ServiceId = 1,
            CustomerId = "cust-1",
            BookingStart = existingBookingStart,
            Status = BookingStatus.Confirmed
        };
        
        await dbContext.Bookings.AddAsync(existingBooking);
        await dbContext.SaveChangesAsync();

        var overlappingBookingStart = existingBookingStart.AddMinutes(30);

        // Act:
        var result = await availabilityService.IsSlotAvailableAsync(1, overlappingBookingStart, 60);

        // Assert:
        Assert.False(result);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenSlotIsAdjacentToExistingBooking_ShouldReturnTrue()
    {
        // Arrange:
        await SeedServiceAndProvider();
        var operatingHour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        
        await dbContext.OperatingHours.AddAsync(operatingHour);

        var nextMonday = DateTime.UtcNow.AddDays(1);
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        var existingBookingStart = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 10, 0, 0, DateTimeKind.Utc);
        var existingBooking = new Booking
        {
            ServiceId = 1,
            CustomerId = "cust-1",
            BookingStart = existingBookingStart,
            Status = BookingStatus.Confirmed
        };
        
        await dbContext.Bookings.AddAsync(existingBooking);
        await dbContext.SaveChangesAsync();

        var adjacentBookingStart = existingBookingStart.AddMinutes(60);

        // Act:
        var result = await availabilityService.IsSlotAvailableAsync(1, adjacentBookingStart, 60);

        // Assert:
        Assert.True(result);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenSlotOverlapsWithCancelledBooking_ShouldReturnTrue()
    {
        // Arrange:
        await SeedServiceAndProvider();
        var operatingHour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        await dbContext.OperatingHours.AddAsync(operatingHour);

        var nextMonday = DateTime.UtcNow.AddDays(1);
        
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        
        var cancelledBookingStart = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 10, 0, 0, DateTimeKind.Utc);
        var cancelledBooking = new Booking
        {
            ServiceId = 1,
            CustomerId = "cust-1",
            BookingStart = cancelledBookingStart,
            Status = BookingStatus.Cancelled
        };
        
        await dbContext.Bookings.AddAsync(cancelledBooking);
        await dbContext.SaveChangesAsync();

        // Try to book the same slot as the cancelled one
        // Act:
        var result = await availabilityService.IsSlotAvailableAsync(1, cancelledBookingStart, 60);

        // Assert:
        Assert.True(result);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenSlotIsAvailable_ShouldReturnTrue()
    {
        // Arrange:
        await SeedServiceAndProvider();
        var operatingHour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };
        
        await dbContext.OperatingHours.AddAsync(operatingHour);
        await dbContext.SaveChangesAsync();

        var nextMonday = DateTime.UtcNow.AddDays(1);
        
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }
        
        var bookingTime = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 14, 0, 0, DateTimeKind.Utc);

        // Act:
        var result = await availabilityService.IsSlotAvailableAsync(1, bookingTime, 60);

        // Assert:
        Assert.True(result);
    }

    [Fact]
    public async Task IsSlotAvailableAsync_WhenProviderHasSplitShift_ShouldAllowBookingInBothShifts()
    {
        // Arrange:
        await SeedServiceAndProvider();
        var morningShift = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0)
        };
        
        var afternoonShift = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(17, 0)
        };
        
        await dbContext.OperatingHours.AddRangeAsync(morningShift, afternoonShift);
        await dbContext.SaveChangesAsync();

        var nextMonday = DateTime.UtcNow.AddDays(1);
        
        while (nextMonday.DayOfWeek != DayOfWeek.Monday)
        {
            nextMonday = nextMonday.AddDays(1);
        }

        var morningBooking = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 10, 0, 0, DateTimeKind.Utc);
        var lunchBooking = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 12, 0, 0, DateTimeKind.Utc);
        var afternoonBooking = new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 14, 0, 0, DateTimeKind.Utc);

        // Act:
        var isMorningAvailable = await availabilityService.IsSlotAvailableAsync(1, morningBooking, 60);
        var isLunchAvailable = await availabilityService.IsSlotAvailableAsync(1, lunchBooking, 60);
        var isAfternoonAvailable = await availabilityService.IsSlotAvailableAsync(1, afternoonBooking, 60);

        // Assert:
        Assert.True(isMorningAvailable, "Morning slot should be available");
        Assert.False(isLunchAvailable, "Lunch slot should be unavailable");
        Assert.True(isAfternoonAvailable, "Afternoon slot should be available");
    }
}