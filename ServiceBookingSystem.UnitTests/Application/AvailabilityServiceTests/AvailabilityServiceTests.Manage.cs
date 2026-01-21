using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Application.DTOs.Availability;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.UnitTests.Application.AvailabilityServiceTests;

public partial class AvailabilityServiceTests
{
    [Fact]
    public async Task GetWeeklyScheduleAsync_WhenNoHoursExist_ShouldReturn7DaysClosed()
    {
        // Arrange
        await SeedServiceAndProvider();

        // Act
        var result = await availabilityService.GetWeeklyScheduleAsync(1);

        // Assert
        Assert.Equal(7, result.Days.Count);
        Assert.All(result.Days, d => Assert.True(d.IsClosed));
        Assert.All(result.Days, d => Assert.Empty(d.Segments));
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_WhenHoursExist_ShouldReturnCorrectStructure()
    {
        // Arrange
        await SeedServiceAndProvider();
        var hour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            Segments = new List<OperatingSegment>
            {
                new() { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) }
            }
        };
        await dbContext.OperatingHours.AddAsync(hour);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await availabilityService.GetWeeklyScheduleAsync(1);

        // Assert
        var monday = result.Days.First(d => d.DayOfWeek == DayOfWeek.Monday);
        Assert.False(monday.IsClosed);
        Assert.Single(monday.Segments);
        Assert.Equal(new TimeOnly(9, 0), monday.Segments[0].Start);
        
        var tuesday = result.Days.First(d => d.DayOfWeek == DayOfWeek.Tuesday);
        Assert.True(tuesday.IsClosed);
    }

    [Fact]
    public async Task UpdateWeeklyScheduleAsync_AsOwner_ShouldReplaceSchedule()
    {
        // Arrange
        await SeedServiceAndProvider();
        // Existing: Monday 9-17
        var existingHour = new OperatingHour
        {
            ServiceId = 1,
            DayOfWeek = DayOfWeek.Monday,
            Segments = new List<OperatingSegment> { new() { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) } }
        };
        await dbContext.OperatingHours.AddAsync(existingHour);
        await dbContext.SaveChangesAsync();

        var newSchedule = new WeeklyScheduleDto();
        // New: Monday Closed, Tuesday 10-14
        newSchedule.Days.Add(new DayScheduleDto { DayOfWeek = DayOfWeek.Monday, IsClosed = true });
        newSchedule.Days.Add(new DayScheduleDto 
        { 
            DayOfWeek = DayOfWeek.Tuesday, 
            IsClosed = false,
            Segments = new List<TimeSegmentDto> { new() { Start = new TimeOnly(10, 0), End = new TimeOnly(14, 0) } }
        });

        // Act
        await availabilityService.UpdateWeeklyScheduleAsync(1, newSchedule, "provider-1");

        // Assert
        dbContext.ChangeTracker.Clear();
        var hours = await dbContext.OperatingHours.Include(h => h.Segments).Where(h => h.ServiceId == 1).ToListAsync();
        
        Assert.Single(hours); // Only Tuesday should exist (Monday is closed/removed)
        var tuesday = hours.First();
        Assert.Equal(DayOfWeek.Tuesday, tuesday.DayOfWeek);
        Assert.Single(tuesday.Segments);
        Assert.Equal(new TimeOnly(10, 0), tuesday.Segments.First().StartTime);
    }

    [Fact]
    public async Task UpdateWeeklyScheduleAsync_AsNonOwner_ShouldThrowAuthorizationException()
    {
        // Arrange
        await SeedServiceAndProvider();
        var schedule = new WeeklyScheduleDto();

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            availabilityService.UpdateWeeklyScheduleAsync(1, schedule, "attacker"));
    }

    [Fact]
    public async Task AddOverrideAsync_AsOwner_ShouldAddOverride()
    {
        // Arrange
        await SeedServiceAndProvider();
        var overrideDto = new ScheduleOverrideDto
        {
            Date = new DateOnly(2024, 12, 25),
            IsDayOff = true
        };

        // Act
        await availabilityService.AddOverrideAsync(1, overrideDto, "provider-1");

        // Assert
        dbContext.ChangeTracker.Clear();
        var savedOverride = await dbContext.ScheduleOverrides.FirstOrDefaultAsync(o => o.ServiceId == 1);
        Assert.NotNull(savedOverride);
        Assert.Equal(new DateOnly(2024, 12, 25), savedOverride.Date);
        Assert.True(savedOverride.IsDayOff);
    }

    [Fact]
    public async Task AddOverrideAsync_AsNonOwner_ShouldThrowAuthorizationException()
    {
        // Arrange
        await SeedServiceAndProvider();
        var overrideDto = new ScheduleOverrideDto { Date = new DateOnly(2024, 12, 25) };

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            availabilityService.AddOverrideAsync(1, overrideDto, "attacker"));
    }

    [Fact]
    public async Task DeleteOverrideAsync_AsOwner_ShouldRemoveOverride()
    {
        // Arrange
        await SeedServiceAndProvider();
        var overrideEntity = new ScheduleOverride
        {
            ServiceId = 1,
            Date = new DateOnly(2024, 12, 25),
            IsDayOff = true
        };
        await dbContext.ScheduleOverrides.AddAsync(overrideEntity);
        await dbContext.SaveChangesAsync();

        // Act
        await availabilityService.DeleteOverrideAsync(overrideEntity.Id, "provider-1");

        // Assert
        dbContext.ChangeTracker.Clear();
        var deleted = await dbContext.ScheduleOverrides.FindAsync(overrideEntity.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteOverrideAsync_AsNonOwner_ShouldThrowAuthorizationException()
    {
        // Arrange
        await SeedServiceAndProvider();
        var overrideEntity = new ScheduleOverride
        {
            ServiceId = 1,
            Date = new DateOnly(2024, 12, 25)
        };
        await dbContext.ScheduleOverrides.AddAsync(overrideEntity);
        await dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(() =>
            availabilityService.DeleteOverrideAsync(overrideEntity.Id, "attacker"));
    }
}
