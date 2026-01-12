using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

[Authorize(Roles = RoleConstants.Customer)]
[Route("[controller]")]
public class BookingController : Controller
{
    private readonly IBookingService bookingService;
    private readonly IServiceService serviceService;
    private readonly ILogger<BookingController> logger;

    public BookingController(
        IBookingService bookingService,
        IServiceService serviceService,
        ILogger<BookingController> logger)
    {
        this.bookingService = bookingService;
        this.serviceService = serviceService;
        this.logger = logger;
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(int serviceId)
    {
        var service = await serviceService.GetServiceByIdAsync(serviceId);
        if (service == null)
        {
            return NotFound();
        }

        var model = new BookingCreateViewModel
        {
            ServiceId = service.Id,
            ServiceName = service.Name,
            ServicePrice = service.Price,
            ProviderName = service.ProviderName
        };

        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var dto = new BookingCreateDto
            {
                ServiceId = model.ServiceId,
                BookingStart = model.GetBookingStart(),
                Notes = model.Notes
            };

            var booking = await bookingService.CreateBookingAsync(dto, userId);
            
            return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create booking.");
            ModelState.AddModelError(string.Empty, ex.Message); // Show service error (e.g. "Slot taken")
            return View(model);
        }
    }

    [HttpGet("Confirmation/{id}")]
    public async Task<IActionResult> Confirmation(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var booking = await bookingService.GetBookingByIdAsync(id, userId);
        if (booking == null)
        {
            return NotFound();
        }

        var model = new BookingConfirmationViewModel
        {
            BookingId = booking.Id,
            ServiceName = booking.ServiceName,
            ProviderName = booking.ProviderName,
            BookingStart = booking.BookingStart,
            Status = booking.Status,
            Price = booking.ServicePrice,
            Notes = booking.Notes
        };

        return View(model);
    }
}