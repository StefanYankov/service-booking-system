using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Defines the contract for checking service availability and retrieving open time slots.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Checks if a specific time slot is available for a given service.
    /// This includes checking operating hours and conflicting bookings.
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
    /// Calculates slots based on the service duration, operating hours, and existing bookings.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="date">The date to check for availability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of available start times (TimeOnly).</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the service is not found.</exception>
    Task<IEnumerable<TimeOnly>> GetAvailableSlotsAsync(int serviceId, DateTime date, CancellationToken cancellationToken = default);
}