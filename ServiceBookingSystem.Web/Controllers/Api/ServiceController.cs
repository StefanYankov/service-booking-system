using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.Web.Controllers.Api;

public class ServiceController : BaseApiController
{
    private readonly IServiceService serviceService;
    private readonly ILogger<ServiceController> logger;

    public ServiceController(
        IServiceService serviceService,
        ILogger<ServiceController> logger)
    {
        this.serviceService = serviceService;
        this.logger = logger;
    }

    /// <summary>
    /// Retrieves a service by its ID.
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceViewDto>> GetById(int id, CancellationToken cancellationToken)
    {
        logger
            .LogDebug("API: GetById request for Service {ServiceId}", id);
        var service = await this.serviceService.GetServiceByIdAsync(id, cancellationToken);
        
        if (service == null)
        {
            return NotFound();
        }

        return Ok(service);
    }

    /// <summary>
    /// Searches for services based on various criteria.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ServiceViewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceViewDto>>> Search(
        [FromQuery] ServiceSearchParameters parameters, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("API: Search services request");
        var result = await this.serviceService.SearchServicesAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a list of distinct cities where services are offered.
    /// </summary>
    [HttpGet("cities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetCities(CancellationToken cancellationToken)
    {
        logger.LogDebug("API: GetCities request");
        var result = await this.serviceService.GetDistinctCitiesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves services by category with pagination.
    /// </summary>
    [HttpGet("category/{categoryId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ServiceViewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceViewDto>>> GetByCategory(
        int categoryId, 
        [FromQuery] PagingAndSortingParameters parameters, 
        CancellationToken cancellationToken)
    {
        logger.LogDebug("API: GetByCategory request for Category {CategoryId}", categoryId);
        var result = await this.serviceService.GetServicesByCategoryAsync(categoryId, parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves services by provider with pagination.
    /// </summary>
    [HttpGet("provider/{providerId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ServiceViewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceViewDto>>> GetByProvider(
        string providerId, 
        [FromQuery] PagingAndSortingParameters parameters, 
        CancellationToken cancellationToken)
    {
        logger
            .LogDebug("API: GetByProvider request for Provider {ProviderId}", providerId);
        var result = await this.serviceService.GetServicesByProviderAsync(providerId, parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new service. Requires Provider role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Provider)]
    [ProducesResponseType(typeof(ServiceViewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServiceViewDto>> Create([FromBody] ServiceCreateDto dto, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: Create service request by User {UserId}", userId);

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.serviceService.CreateServiceAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing service. Only the owner can update.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Provider)]
    [ProducesResponseType(typeof(ServiceViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceViewDto>> Update(int id, [FromBody] ServiceUpdateDto dto, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: Update service {ServiceId} request by User {UserId}", id, userId);

        if (userId == null)
        {
            return Unauthorized();
        }

        if (id != dto.Id)
        {
            ModelState.AddModelError("Id", "The ID in the URL does not match the ID in the body.");
            return BadRequest(ModelState);
        }

        var result = await this.serviceService.UpdateServiceAsync(dto, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a service. Only the owner can delete.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Provider)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: Delete service {ServiceId} request by User {UserId}", id, userId);

        if (userId == null)
        {
            return Unauthorized();
        }

        await this.serviceService.DeleteServiceAsync(id, userId, cancellationToken);
        return NoContent();
    }
}