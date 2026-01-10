using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        var bookingDayOfWeek = bookingStart.DayOfWeek;

        var operatingHours = await dbContext.OperatingHours
            .AsNoTracking()
            .Where(oh => oh.ServiceId == serviceId && oh.DayOfWeek == bookingDayOfWeek)
            .ToListAsync(cancellationToken);

        // If there are no operating hours at all for that day, the slot is unavailable.
        if (!operatingHours.Any())
        {
            logger
                .LogInformation("Availability check failed for ServiceId {ServiceId}: No operating hours defined for {DayOfWeek}.",
                    serviceId, bookingDayOfWeek);
            return false;
        }

        var fitsInAnySlot = operatingHours.Any(oh =>
            bookingStartTime >= oh.StartTime && bookingEndTime <= oh.EndTime);

        if (!fitsInAnySlot)
        {
            logger
                .LogInformation("Availability check failed for ServiceId {ServiceId}: Proposed time is outside of all operating hours.",
                    serviceId);
            return false;
        }

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
                .LogInformation("Availability check failed for ServiceId {ServiceId}: A conflicting booking exists for the proposed time.",
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

        var dayOfWeek = date.DayOfWeek;
        var operatingHours = await dbContext.OperatingHours
            .AsNoTracking()
            .Where(oh => oh.ServiceId == serviceId && oh.DayOfWeek == dayOfWeek)
            .OrderBy(oh => oh.StartTime)
            .ToListAsync(cancellationToken);

        if (!operatingHours.Any())
        {
            logger.
                LogInformation("No operating hours found for ServiceId {ServiceId} on {DayOfWeek}.",
                    serviceId, dayOfWeek);
            return Enumerable.Empty<TimeOnly>();
        }

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

        foreach (var hours in operatingHours)
        {
            var currentSlotStart = hours.StartTime;
            var shiftEnd = hours.EndTime;

            // Loop until the slot + duration exceeds the shift end
            while (currentSlotStart.Add(duration) <= shiftEnd)
            {
                var currentSlotEnd = currentSlotStart.Add(duration);
                var currentSlotDateTimeStart = dayStart.Add(currentSlotStart.ToTimeSpan());
                var currentSlotDateTimeEnd = dayStart.Add(currentSlotEnd.ToTimeSpan());

                // Filter out past slots if the date is today
                if (currentSlotDateTimeStart < DateTime.UtcNow)
                {
                    currentSlotStart = currentSlotEnd; // Move to next slot
                    continue;
                }

                // Check for conflicts
                bool isConflict = existingBookings.Any(b =>
                {
                    var bookingStart = b.BookingStart;
                    var bookingEnd = b.BookingStart.AddMinutes(b.Duration);

                    // Overlap logic: (StartA < EndB) and (EndA > StartB)
                    return currentSlotDateTimeStart < bookingEnd && currentSlotDateTimeEnd > bookingStart;
                });

                if (!isConflict)
                {
                    availableSlots.Add(currentSlotStart);
                }

                // Move to next slot (Step = Duration)
                currentSlotStart = currentSlotEnd;
            }
        }

        return availableSlots;
    }
}