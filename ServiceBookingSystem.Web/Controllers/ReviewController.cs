using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

[Authorize(Roles = RoleConstants.Customer)]
[Route("[controller]")]
public class ReviewController : Controller
{
    private readonly IReviewService reviewService;
    private readonly IBookingService bookingService;
    private readonly ILogger<ReviewController> logger;

    public ReviewController(
        IReviewService reviewService,
        IBookingService bookingService,
        ILogger<ReviewController> logger)
    {
        this.reviewService = reviewService;
        this.bookingService = bookingService;
        this.logger = logger;
    }

    [HttpGet("Create/{bookingId}")]
    public async Task<IActionResult> Create(string bookingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var booking = await bookingService.GetBookingByIdAsync(bookingId, userId);
        if (booking == null)
        {
            return NotFound();
        }

        if (booking.Status != BookingStatus.Completed.ToString())
        {
            TempData["ErrorMessage"] = "You can only review completed bookings.";
            return RedirectToAction("Index", "Booking");
        }

        // Check if already reviewed? 
        // IReviewService.HasUserReviewedServiceAsync checks service-level, but maybe we want per-booking?
        // For now, let's assume one review per booking is the flow, but the service enforces one per service?
        // Let's check IReviewService.
        
        var model = new ReviewCreateViewModel
        {
            BookingId = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.ServiceName
        };

        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var dto = new ReviewCreateDto
            {
                BookingId = model.BookingId,
                Rating = model.Rating,
                Comment = model.Comment
            };

            await reviewService.CreateReviewAsync(dto, userId);
            
            TempData["SuccessMessage"] = "Review submitted successfully!";
            return RedirectToAction("Details", "Service", new { id = model.ServiceId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit review.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}