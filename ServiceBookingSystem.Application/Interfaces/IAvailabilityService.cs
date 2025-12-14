using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

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
}