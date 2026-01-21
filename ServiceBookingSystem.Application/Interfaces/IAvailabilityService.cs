using ServiceBookingSystem.Application.DTOs.Availability;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Defines the contract for checking service availability and retrieving open time slots.
/// Handles complex scheduling logic including weekly operating hours, holidays, and custom date overrides.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Checks if a specific time slot is available for a given service.
    /// This includes checking operating hours (or overrides), holidays, and conflicting bookings.
    /// </summary>
    /// <param name="serviceId">The ID of the service to check.</param>
    /// <param name="bookingStart">The proposed start time of the booking.</param>
    /// <param name="durationMinutes">The duration of the service in minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the slot is available, false otherwise.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the service with the specified ID does not exist.</exception>
    Task<bool> IsSlotAvailableAsync(int serviceId, DateTime bookingStart, int durationMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all available start times for a service on a specific date.
    /// Calculates slots based on the service duration, operating hours (or overrides), and existing bookings.
    /// Returns an empty list if the date is a holiday or outside of operating hours.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="date">The date to check for availability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of available start times (TimeOnly).</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the service is not found.</exception>
    Task<IEnumerable<TimeOnly>> GetAvailableSlotsAsync(int serviceId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the weekly schedule configuration for a service.
    /// Returns a list of 7 days, each with its operating segments (shifts).
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weekly schedule DTO.</returns>
    Task<WeeklyScheduleDto> GetWeeklyScheduleAsync(int serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the weekly schedule for a service.
    /// Replaces the existing configuration with the new one provided.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="schedule">The new schedule configuration.</param>
    /// <param name="providerId">The ID of the provider performing the update (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="AuthorizationException">Thrown if the provider is not the owner of the service.</exception>
    Task UpdateWeeklyScheduleAsync(int serviceId, WeeklyScheduleDto schedule, string providerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all schedule overrides (holidays and custom hours) for a service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of schedule overrides.</returns>
    Task<List<ScheduleOverrideDto>> GetOverridesAsync(int serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new schedule override (holiday or custom hours) for a specific date.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="overrideDto">The override details.</param>
    /// <param name="providerId">The ID of the provider performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddOverrideAsync(int serviceId, ScheduleOverrideDto overrideDto, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schedule override.
    /// </summary>
    /// <param name="overrideId">The ID of the override to delete.</param>
    /// <param name="providerId">The ID of the provider performing the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteOverrideAsync(int overrideId, string providerId, CancellationToken cancellationToken = default);
}
