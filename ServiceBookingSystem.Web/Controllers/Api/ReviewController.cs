using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// Manages review operations including creation, updates, and retrieval.
/// </summary>
public class ReviewController : BaseApiController
{
    private readonly IReviewService reviewService;
    private readonly ILogger<ReviewController> logger;

    public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
    {
        this.reviewService = reviewService;
        this.logger = logger;
    }

    /// <summary>
    /// Retrieves a specific review by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the review.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReviewViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewViewDto>> GetById(int id, CancellationToken cancellationToken)
    {
        logger
            .LogDebug("API: GetById request received for Review {ReviewId}",
                id);

        var review = await this.reviewService.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            return NotFound();
        }

        return Ok(review);
    }

    /// <summary>
    /// Creates a new review for a completed booking.
    /// </summary>
    /// <remarks>
    /// Users can only review services they have booked and completed.
    /// </remarks>
    /// <param name="dto">The review details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ReviewViewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewViewDto>> Create([FromBody] ReviewCreateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: Create review request received from user {UserId}",
                userId);

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.reviewService.CreateReviewAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing review.
    /// </summary>
    /// <param name="dto">The updated review details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReviewViewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewViewDto>> Update([FromBody] ReviewUpdateDto dto, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: Review update request received from user {UserId} for review {ReviewId}",
                userId, dto.Id);
        
        if (userId == null)
        {
            return Unauthorized();
        }
        
        var result = await this.reviewService.UpdateReviewAsync(dto, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a paginated list of reviews for a specific service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="parameters">Pagination and sorting options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpGet("/api/services/{serviceId:int}/reviews")]
    [ProducesResponseType(typeof(PagedResult<ReviewViewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ReviewViewDto>>> GetReviewsByService(int serviceId,
        [FromQuery] PagingAndSortingParameters parameters, CancellationToken cancellationToken)
    {
        logger
            .LogDebug("API: GetReviewsByService request received for Service {ServiceId}",
                serviceId);

        var result = await this.reviewService.GetReviewsByServiceAsync(serviceId ,parameters, cancellationToken);
        return Ok(result);
    }
    
    /// <summary>
    /// Retrieves the summary (average rating, count) of reviews for a specific service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpGet("/api/services/{serviceId:int}/reviews/summary")]
    [ProducesResponseType(typeof(ReviewSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewSummaryDto>> GetReviewSummaryByService(int serviceId,
        CancellationToken cancellationToken)
    {
        logger
            .LogDebug("API: GetReviewSummaryByService request received for Service {ServiceId}",
                serviceId);

        var result = await this.reviewService.GetReviewSummaryAsync(serviceId, cancellationToken);
        return Ok(result);
    }
}