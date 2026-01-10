using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.Interfaces;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// Provides endpoints for checking service availability and retrieving open time slots.
/// </summary>
public class AvailabilityController : BaseApiController
{
    private readonly IAvailabilityService availabilityService;
    private readonly ILogger<AvailabilityController> logger;

    public AvailabilityController(
        IAvailabilityService availabilityService,
        ILogger<AvailabilityController> logger)
    {
        this.availabilityService = availabilityService;
        this.logger = logger;
    }

    /// <summary>
    /// Retrieves all available time slots for a specific service on a given date.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="date">The date to check (YYYY-MM-DD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available start times.</returns>
    [AllowAnonymous]
    [HttpGet("slots")]
    [ProducesResponseType(typeof(IEnumerable<TimeOnly>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TimeOnly>>> GetAvailableSlots(
        [FromQuery] int serviceId, 
        [FromQuery] DateTime date, 
        CancellationToken cancellationToken)
    {
        logger
            .LogDebug("API: GetAvailableSlots request for Service {ServiceId} on {Date}",
                serviceId, date);

        var slots = await this.availabilityService.GetAvailableSlotsAsync(serviceId, date, cancellationToken);
        return Ok(slots);
    }

    /// <summary>
    /// Checks if a specific time slot is available for booking.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="bookingStart">The proposed start time.</param>
    /// <param name="durationMinutes">The duration in minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if available, otherwise false.</returns>
    [AllowAnonymous]
    [HttpGet("check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> CheckSlot(
        [FromQuery] int serviceId, 
        [FromQuery] DateTime bookingStart, 
        [FromQuery] int durationMinutes, 
        CancellationToken cancellationToken)
    {
        logger
            .LogDebug("API: CheckSlot request for Service {ServiceId} at {Start}",
                serviceId, bookingStart);

        var isAvailable = await this.availabilityService.IsSlotAvailableAsync(serviceId, bookingStart, durationMinutes, cancellationToken);
        return Ok(isAvailable);
    }
}