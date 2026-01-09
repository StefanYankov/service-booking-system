using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;

namespace ServiceBookingSystem.Web.Controllers.Api;

[Authorize]
public class BookingController : BaseApiController
{
    private readonly IBookingService bookingService;
    private readonly ILogger<BookingController> logger;

    public BookingController(
        IBookingService bookingService,
        ILogger<BookingController> logger)
    {
        this.bookingService = bookingService;
        this.logger = logger;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookingViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingViewDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: GetById request received for Booking {BookingId} by user {UserId}",
                id, userId);
        
        if (userId == null)
        {
            return Unauthorized();
        }

        var booking = await this.bookingService.GetBookingByIdAsync(id, userId, cancellationToken);
        if (booking == null)
        {
            return NotFound();
        }

        return Ok(booking);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookingViewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingViewDto>> Create([FromBody] BookingCreateDto dto,
        CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: Create booking request received from user {UserId}",
                userId);
        
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.bookingService.CreateBookingAsync(dto, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("my-bookings")]
    [ProducesResponseType(typeof(PagedResult<BookingViewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<BookingViewDto>>> GetMyBookings(
        [FromQuery] PagingAndSortingParameters parameters,
        CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: GetMyBookings request received from user {UserId}",
                userId);
        
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.bookingService.GetBookingsByCustomerAsync(userId, parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("received")]
    [ProducesResponseType(typeof(PagedResult<BookingViewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<BookingViewDto>>> GetReceivedBookings(
        [FromQuery] PagingAndSortingParameters parameters, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: GetReceivedBookings request received from user {UserId}",
                userId);
        
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.bookingService.GetBookingsByProviderAsync(userId, parameters, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/cancel")]
    [ProducesResponseType(typeof(BookingViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingViewDto>> Cancel(string id, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: Cancel booking {BookingId} request received from user {UserId}",
                id, userId);
        
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.bookingService.CancelBookingAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/confirm")]
    [ProducesResponseType(typeof(BookingViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingViewDto>> Confirm(string id, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: Confirm booking {BookingId} request received from user {UserId}",
                id, userId);
        
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.bookingService.ConfirmBookingAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/decline")]
    [ProducesResponseType(typeof(BookingViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingViewDto>> Decline(string id, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: Decline booking {BookingId} request received from user {UserId}",
                id, userId);
        
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.bookingService.DeclineBookingAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/complete")]
    [ProducesResponseType(typeof(BookingViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookingViewDto>> Complete(string id, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: Complete booking {BookingId} request received from user {UserId}",
                id, userId);

        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await this.bookingService.CompleteBookingAsync(id, userId, cancellationToken);
        return Ok(result);
    }
}