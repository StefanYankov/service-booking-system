using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.DTOs.Availability;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<AvailabilityService> logger;

    public AvailabilityService(
        ApplicationDbContext dbContext,
        ILogger<AvailabilityService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> IsSlotAvailableAsync(int serviceId, DateTime bookingStart, int durationMinutes, CancellationToken cancellationToken = default)
    {
        logger
            .LogDebug("Checking availability for ServiceId {ServiceId} at {BookingStart} for {Duration} minutes.",
                serviceId, bookingStart, durationMinutes);

        var serviceExists = await dbContext.Services.AnyAsync(s => s.Id == serviceId, cancellationToken);
        if (!serviceExists)
        {
            logger.LogWarning("Availability check failed: Service with ID {ServiceId} not found.", serviceId);
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }

        if (bookingStart < DateTime.UtcNow)
        {
            logger
                .LogWarning("Availability check failed for ServiceId {ServiceId}: Proposed time {BookingStart} is in the past.",
                    serviceId, bookingStart);
            return false;
        }

        var bookingEnd = bookingStart.AddMinutes(durationMinutes);
        var bookingStartTime = TimeOnly.FromDateTime(bookingStart);
        var bookingEndTime = TimeOnly.FromDateTime(bookingEnd);
        var bookingDate = DateOnly.FromDateTime(bookingStart);
        var bookingDayOfWeek = bookingStart.DayOfWeek;

        // 1. Check for Override
        var overrideSchedule = await dbContext.ScheduleOverrides
            .AsNoTracking()
            .Include(o => o.Segments)
            .FirstOrDefaultAsync(o => o.ServiceId == serviceId && o.Date == bookingDate, cancellationToken);

        List<(TimeOnly Start, TimeOnly End)> activeSegments = new();

        if (overrideSchedule != null)
        {
            if (overrideSchedule.IsDayOff)
            {
                logger.LogInformation("Availability check failed for ServiceId {ServiceId}: Date {Date} is a holiday/day off.", serviceId, bookingDate);
                return false;
            }
            activeSegments = overrideSchedule.Segments.Select(s => (s.StartTime, s.EndTime)).ToList();
        }
        else
        {
            // 2. Fallback to Weekly Schedule
            var operatingHour = await dbContext.OperatingHours
                .AsNoTracking()
                .Include(oh => oh.Segments)
                .FirstOrDefaultAsync(oh => oh.ServiceId == serviceId && oh.DayOfWeek == bookingDayOfWeek, cancellationToken);

            if (operatingHour == null || !operatingHour.Segments.Any())
            {
                logger.LogInformation("Availability check failed for ServiceId {ServiceId}: No operating hours for {DayOfWeek}.", serviceId, bookingDayOfWeek);
                return false;
            }
            activeSegments = operatingHour.Segments.Select(s => (s.StartTime, s.EndTime)).ToList();
        }

        // 3. Check if slot fits in any segment
        var fitsInAnySlot = activeSegments.Any(seg =>
            bookingStartTime >= seg.Start && bookingEndTime <= seg.End);

        if (!fitsInAnySlot)
        {
            logger
                .LogInformation("Availability check failed for ServiceId {ServiceId}: Proposed time is outside of defined segments.",
                    serviceId);
            return false;
        }

        // 4. Check for Conflicting Bookings
        var conflictingBookingExists = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.ServiceId == serviceId)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)
            .AnyAsync(b =>
                bookingStart < b.BookingStart.AddMinutes(b.Service.DurationInMinutes) &&
                bookingEnd > b.BookingStart,
                cancellationToken);

        if (conflictingBookingExists)
        {
            logger
                .LogInformation("Availability check failed for ServiceId {ServiceId}: A conflicting booking exists.",
                    serviceId);
            return false;
        }

        logger
            .LogInformation("Availability check successful for ServiceId {ServiceId} at {BookingStart}.",
                serviceId, bookingStart);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TimeOnly>> GetAvailableSlotsAsync(int serviceId, DateTime date, CancellationToken cancellationToken = default)
    {
        logger
            .LogDebug("Fetching available slots for ServiceId {ServiceId} on {Date}",
                serviceId, date.Date);

        var service = await dbContext.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);

        if (service == null)
        {
            logger
                .LogWarning("Service with ID {ServiceId} not found.",
                    serviceId);
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }

        var targetDate = DateOnly.FromDateTime(date);
        var dayOfWeek = date.DayOfWeek;

        // 1. Determine Segments (Override vs Weekly)
        var overrideSchedule = await dbContext.ScheduleOverrides
            .AsNoTracking()
            .Include(o => o.Segments)
            .FirstOrDefaultAsync(o => o.ServiceId == serviceId && o.Date == targetDate, cancellationToken);

        List<(TimeOnly Start, TimeOnly End)> activeSegments = new();

        if (overrideSchedule != null)
        {
            if (overrideSchedule.IsDayOff)
            {
                logger.LogInformation("Date {Date} is a holiday for ServiceId {ServiceId}. No slots.", targetDate, serviceId);
                return Enumerable.Empty<TimeOnly>();
            }
            activeSegments = overrideSchedule.Segments.Select(s => (s.StartTime, s.EndTime)).ToList();
        }
        else
        {
            var operatingHour = await dbContext.OperatingHours
                .AsNoTracking()
                .Include(oh => oh.Segments)
                .FirstOrDefaultAsync(oh => oh.ServiceId == serviceId && oh.DayOfWeek == dayOfWeek, cancellationToken);

            if (operatingHour == null || !operatingHour.Segments.Any())
            {
                logger.LogInformation("No operating hours for ServiceId {ServiceId} on {DayOfWeek}.", serviceId, dayOfWeek);
                return Enumerable.Empty<TimeOnly>();
            }
            activeSegments = operatingHour.Segments.Select(s => (s.StartTime, s.EndTime)).ToList();
        }

        // 2. Fetch Existing Bookings
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var existingBookings = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.ServiceId == serviceId)
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)
            .Where(b => b.BookingStart >= dayStart && b.BookingStart < dayEnd)
            .Select(b => new 
                { 
                    b.BookingStart,
                    Duration = b.Service.DurationInMinutes
                })
            .ToListAsync(cancellationToken);

        var availableSlots = new List<TimeOnly>();
        var duration = TimeSpan.FromMinutes(service.DurationInMinutes);

        // 3. Generate Slots for Each Segment
        foreach (var segment in activeSegments)
        {
            var currentSlotStartTs = segment.Start.ToTimeSpan();
            var shiftEndTs = segment.End.ToTimeSpan();
            
            if (shiftEndTs < currentSlotStartTs) shiftEndTs = shiftEndTs.Add(TimeSpan.FromDays(1));

            while (currentSlotStartTs.Add(duration) <= shiftEndTs)
            {
                var currentSlotEndTs = currentSlotStartTs.Add(duration);
                var currentSlotDateTimeStart = dayStart.Add(currentSlotStartTs);
                var currentSlotDateTimeEnd = dayStart.Add(currentSlotEndTs);

                // Filter out past slots if the date is today
                if (currentSlotDateTimeStart < DateTime.UtcNow)
                {
                    currentSlotStartTs = currentSlotEndTs;
                    continue;
                }

                // Check for conflicts
                bool isConflict = existingBookings.Any(b =>
                {
                    var bookingStart = b.BookingStart;
                    var bookingEnd = b.BookingStart.AddMinutes(b.Duration);

                    return currentSlotDateTimeStart < bookingEnd && currentSlotDateTimeEnd > bookingStart;
                });

                if (!isConflict)
                {
                    availableSlots.Add(TimeOnly.FromTimeSpan(currentSlotStartTs));
                }

                // Move to next slot
                currentSlotStartTs = currentSlotEndTs;
            }
        }

        return availableSlots.OrderBy(t => t);
    }

    // --- Management Methods ---

    public async Task<WeeklyScheduleDto> GetWeeklyScheduleAsync(int serviceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching weekly schedule for ServiceId {ServiceId}", serviceId);

        var hours = await dbContext.OperatingHours
            .AsNoTracking()
            .Include(oh => oh.Segments)
            .Where(oh => oh.ServiceId == serviceId)
            .ToListAsync(cancellationToken);

        var dto = new WeeklyScheduleDto();
        
        // Ensure all 7 days are present in the DTO, ordered Monday -> Sunday
        var days = Enum.GetValues(typeof(DayOfWeek))
            .Cast<DayOfWeek>()
            .OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d);

        foreach (var day in days)
        {
            var hour = hours.FirstOrDefault(h => h.DayOfWeek == day);
            var dayDto = new DayScheduleDto
            {
                DayOfWeek = day,
                IsClosed = hour == null || !hour.Segments.Any(),
                Segments = hour?.Segments.Select(s => new TimeSegmentDto { Start = s.StartTime, End = s.EndTime }).ToList() ?? new()
            };
            dto.Days.Add(dayDto);
        }

        return dto;
    }

    public async Task UpdateWeeklyScheduleAsync(int serviceId, WeeklyScheduleDto schedule, string providerId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating weekly schedule for ServiceId {ServiceId} by Provider {ProviderId}", serviceId, providerId);

        var service = await dbContext.Services.FindAsync([serviceId], cancellationToken);
        if (service == null)
        {
            logger.LogWarning("UpdateWeeklySchedule failed: Service {ServiceId} not found.", serviceId);
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }
        
        if (service.ProviderId != providerId)
        {
            logger.LogWarning("UpdateWeeklySchedule failed: User {UserId} is not the owner of Service {ServiceId}.", providerId, serviceId);
            throw new AuthorizationException(providerId, "Update Schedule");
        }

        var existingHours = await dbContext.OperatingHours
            .Include(oh => oh.Segments)
            .Where(oh => oh.ServiceId == serviceId)
            .ToListAsync(cancellationToken);

        // Remove all existing hours (simplest strategy for full update)
        dbContext.OperatingHours.RemoveRange(existingHours);
        
        // Add new hours
        foreach (var dayDto in schedule.Days)
        {
            if (dayDto.IsClosed || !dayDto.Segments.Any()) continue;

            var hour = new OperatingHour
            {
                ServiceId = serviceId,
                DayOfWeek = dayDto.DayOfWeek,
                Segments = dayDto.Segments.Select(s => new OperatingSegment
                {
                    StartTime = s.Start,
                    EndTime = s.End
                }).ToList()
            };
            await dbContext.OperatingHours.AddAsync(hour, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Weekly schedule updated for ServiceId {ServiceId}", serviceId);
    }

    public async Task<List<ScheduleOverrideDto>> GetOverridesAsync(int serviceId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching schedule overrides for ServiceId {ServiceId}", serviceId);

        var overrides = await dbContext.ScheduleOverrides
            .AsNoTracking()
            .Include(o => o.Segments)
            .Where(o => o.ServiceId == serviceId)
            .OrderBy(o => o.Date)
            .ToListAsync(cancellationToken);

        return overrides.Select(o => new ScheduleOverrideDto
        {
            Id = o.Id,
            Date = o.Date,
            IsDayOff = o.IsDayOff,
            Segments = o.Segments.Select(s => new TimeSegmentDto { Start = s.StartTime, End = s.EndTime }).ToList()
        }).ToList();
    }

    public async Task AddOverrideAsync(int serviceId, ScheduleOverrideDto overrideDto, string providerId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Adding schedule override for ServiceId {ServiceId} on {Date} by Provider {ProviderId}", serviceId, overrideDto.Date, providerId);

        var service = await dbContext.Services.FindAsync([serviceId], cancellationToken);
        if (service == null)
        {
            logger.LogWarning("AddOverride failed: Service {ServiceId} not found.", serviceId);
            throw new EntityNotFoundException(nameof(Service), serviceId);
        }
        
        if (service.ProviderId != providerId)
        {
            logger.LogWarning("AddOverride failed: User {UserId} is not the owner of Service {ServiceId}.", providerId, serviceId);
            throw new AuthorizationException(providerId, "Add Override");
        }

        var newOverride = new ScheduleOverride
        {
            ServiceId = serviceId,
            Date = overrideDto.Date,
            IsDayOff = overrideDto.IsDayOff,
            Segments = overrideDto.Segments.Select(s => new OverrideSegment
            {
                StartTime = s.Start,
                EndTime = s.End
            }).ToList()
        };

        await dbContext.ScheduleOverrides.AddAsync(newOverride, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Schedule override added for ServiceId {ServiceId} on {Date}", serviceId, overrideDto.Date);
    }

    public async Task DeleteOverrideAsync(int overrideId, string providerId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting schedule override {OverrideId} by Provider {ProviderId}", overrideId, providerId);

        var overrideEntity = await dbContext.ScheduleOverrides
            .Include(o => o.Service)
            .FirstOrDefaultAsync(o => o.Id == overrideId, cancellationToken);

        if (overrideEntity == null)
        {
            logger.LogWarning("DeleteOverride failed: Override {OverrideId} not found.", overrideId);
            throw new EntityNotFoundException(nameof(ScheduleOverride), overrideId);
        }
        
        if (overrideEntity.Service.ProviderId != providerId)
        {
            logger.LogWarning("DeleteOverride failed: User {UserId} is not the owner of Service {ServiceId}.", providerId, overrideEntity.ServiceId);
            throw new AuthorizationException(providerId, "Delete Override");
        }

        dbContext.ScheduleOverrides.Remove(overrideEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Schedule override {OverrideId} deleted", overrideId);
    }
}
