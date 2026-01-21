using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Availability;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// Provides endpoints for checking service availability and managing schedules.
/// </summary>
[Area("Api")]
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

    // --- Schedule Management (Provider Only) ---

    /// <summary>
    /// Retrieves the weekly schedule configuration for a service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The weekly schedule.</returns>
    [Authorize(Roles = RoleConstants.Provider)]
    [HttpGet("services/{serviceId}/weekly")]
    [ProducesResponseType(typeof(WeeklyScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeeklyScheduleDto>> GetWeeklySchedule(int serviceId, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: GetWeeklySchedule request for Service {ServiceId} by User {UserId}", serviceId, userId);
        
        if (userId == null) return Unauthorized();

        var schedule = await availabilityService.GetWeeklyScheduleAsync(serviceId, cancellationToken);
        return Ok(schedule);
    }

    /// <summary>
    /// Updates the weekly schedule for a service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="schedule">The new schedule configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No Content.</returns>
    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPut("services/{serviceId}/weekly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateWeeklySchedule(int serviceId, [FromBody] WeeklyScheduleDto schedule, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: UpdateWeeklySchedule request for Service {ServiceId} by User {UserId}", serviceId, userId);
        
        if (userId == null) return Unauthorized();

        await availabilityService.UpdateWeeklyScheduleAsync(serviceId, schedule, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves all schedule overrides for a service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of overrides.</returns>
    [Authorize(Roles = RoleConstants.Provider)]
    [HttpGet("services/{serviceId}/overrides")]
    [ProducesResponseType(typeof(List<ScheduleOverrideDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ScheduleOverrideDto>>> GetOverrides(int serviceId, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: GetOverrides request for Service {ServiceId} by User {UserId}", serviceId, userId);
        
        if (userId == null) return Unauthorized();

        var overrides = await availabilityService.GetOverridesAsync(serviceId, cancellationToken);
        return Ok(overrides);
    }

    /// <summary>
    /// Adds a new schedule override.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="overrideDto">The override details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No Content.</returns>
    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost("services/{serviceId}/overrides")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddOverride(int serviceId, [FromBody] ScheduleOverrideDto overrideDto, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: AddOverride request for Service {ServiceId} by User {UserId}", serviceId, userId);
        
        if (userId == null) return Unauthorized();

        await availabilityService.AddOverrideAsync(serviceId, overrideDto, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a schedule override.
    /// </summary>
    /// <param name="serviceId">The ID of the service (for routing context).</param>
    /// <param name="overrideId">The ID of the override to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No Content.</returns>
    [Authorize(Roles = RoleConstants.Provider)]
    [HttpDelete("services/{serviceId}/overrides/{overrideId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteOverride(int serviceId, int overrideId, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: DeleteOverride request for Override {OverrideId} in Service {ServiceId} by User {UserId}", overrideId, serviceId, userId);

        if (userId == null) return Unauthorized();

        await availabilityService.DeleteOverrideAsync(overrideId, userId, cancellationToken);
        return NoContent();
    }
}
