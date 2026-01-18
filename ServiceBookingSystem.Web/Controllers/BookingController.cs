using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

[Authorize]
[Route("[controller]")]
public class BookingController : Controller
{
    private readonly IBookingService bookingService;
    private readonly IServiceService serviceService;
    private readonly IAvailabilityService availabilityService;
    private readonly IUsersService usersService;
    private readonly ILogger<BookingController> logger;

    public BookingController(
        IBookingService bookingService,
        IServiceService serviceService,
        IAvailabilityService availabilityService,
        IUsersService usersService,
        ILogger<BookingController> logger)
    {
        this.bookingService = bookingService;
        this.serviceService = serviceService;
        this.availabilityService = availabilityService;
        this.usersService = usersService;
        this.logger = logger;
    }

    [Authorize(Roles = RoleConstants.Customer)]
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

    [Authorize(Roles = RoleConstants.Customer)]
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

    [Authorize(Roles = RoleConstants.Customer)]
    [HttpGet("Confirmation/{id}")]
    public async Task<IActionResult> Confirmation(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

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

    [Authorize(Roles = RoleConstants.Customer)]
    [HttpGet("GetSlots")]
    public async Task<IActionResult> GetSlots(int serviceId, DateTime date)
    {
        var slots = await availabilityService.GetAvailableSlotsAsync(serviceId, date);
        return Json(slots);
    }

    [Authorize(Roles = RoleConstants.Customer)]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = pageNumber,
            PageSize = 10,
            SortBy = "Date",
            SortDirection = "Desc"
        };

        var bookingsResult = await bookingService.GetBookingsByCustomerAsync(userId, parameters);
        
        // Map DTO to ViewModel
        var items = bookingsResult.Items.Select(b => new CustomerBookingViewModel
        {
            Id = b.Id,
            ServiceId = b.ServiceId,
            ServiceName = b.ServiceName,
            ProviderName = b.ProviderName,
            BookingStart = b.BookingStart,
            Status = b.Status,
            Price = b.ServicePrice
        }).ToList();

        var model = new PagedResult<CustomerBookingViewModel>(items, bookingsResult.TotalCount, bookingsResult.PageNumber, bookingsResult.PageSize);
        
        return View(model);
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpGet("Received")]
    public async Task<IActionResult> Received(int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = pageNumber,
            PageSize = 10,
            SortBy = "Date",
            SortDirection = "Desc"
        };

        var bookingsResult = await bookingService.GetBookingsByProviderAsync(userId, parameters);
        
        // Map DTO to ViewModel
        var items = bookingsResult.Items.Select(b => new ProviderBookingViewModel
        {
            Id = b.Id,
            ServiceName = b.ServiceName,
            CustomerName = b.CustomerName,
            CustomerId = b.CustomerId, // Needed for link
            CustomerEmail = b.CustomerEmail,
            CustomerPhone = b.CustomerPhone,
            BookingStart = b.BookingStart,
            Status = b.Status,
            Price = b.ServicePrice
        }).ToList();

        var model = new PagedResult<ProviderBookingViewModel>(items, bookingsResult.TotalCount, bookingsResult.PageNumber, bookingsResult.PageSize);

        return View(model);
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpGet("CustomerDetails/{customerId}")]
    public async Task<IActionResult> CustomerDetails(string customerId)
    {
        var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (providerId == null)
        {
            return Unauthorized();
        }

        // 1. Get Customer Info
        var customer = await usersService.GetUserByIdAsync(customerId);
        if (customer == null)
        {
            return NotFound();
        }

        // 2. Get Bookings between Provider and Customer
        var bookings = await bookingService.GetBookingsByProviderAndCustomerAsync(providerId, customerId);

        // 3. Map to ViewModel
        var model = new ProviderCustomerDetailsViewModel
        {
            CustomerId = customer.Id,
            CustomerName = $"{customer.FirstName} {customer.LastName}",
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Bookings = bookings.Select(b => new ProviderBookingViewModel
            {
                Id = b.Id,
                ServiceName = b.ServiceName,
                BookingStart = b.BookingStart,
                Status = b.Status,
                Price = b.ServicePrice
            }).ToList()
        };

        return View(model);
    }

    [HttpPost("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await bookingService.CancelBookingAsync(id, userId);
            TempData["SuccessMessage"] = "Booking cancelled successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel booking {BookingId}", id);
            TempData["ErrorMessage"] = "Failed to cancel booking.";
        }

        if (User.IsInRole(RoleConstants.Provider))
        {
            return RedirectToAction(nameof(Received));
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost("Confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await bookingService.ConfirmBookingAsync(id, userId);
            TempData["SuccessMessage"] = "Booking confirmed.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to confirm booking {BookingId}", id);
            TempData["ErrorMessage"] = "Failed to confirm booking.";
        }

        return RedirectToAction(nameof(Received));
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost("Decline")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await bookingService.DeclineBookingAsync(id, userId);
            TempData["SuccessMessage"] = "Booking declined.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to decline booking {BookingId}", id);
            TempData["ErrorMessage"] = "Failed to decline booking.";
        }

        return RedirectToAction(nameof(Received));
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost("Complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await bookingService.CompleteBookingAsync(id, userId);
            TempData["SuccessMessage"] = "Booking marked as completed.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete booking {BookingId}", id);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Received));
    }

    [HttpGet("Reschedule/{id}")]
    public async Task<IActionResult> Reschedule(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var booking = await bookingService.GetBookingByIdAsync(id, userId);
        if (booking == null)
        {
            return NotFound();
        }

        // Only allow rescheduling if Pending or Confirmed
        if (booking.Status != BookingStatus.Pending.ToString() && booking.Status != BookingStatus.Confirmed.ToString())
        {
            TempData["ErrorMessage"] = "Cannot reschedule this booking.";
            return RedirectToAction(nameof(Index));
        }

        var model = new RescheduleViewModel
        {
            BookingId = booking.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.ServiceName,
            CurrentStart = booking.BookingStart,
            Date = booking.BookingStart.Date,
            Time = booking.BookingStart.TimeOfDay
        };

        return View(model);
    }

    [HttpPost("Reschedule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(RescheduleViewModel model)
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
            var newStart = model.Date.Add(model.Time);
            
            var dto = new BookingUpdateDto
            {
                Id = model.BookingId,
                BookingStart = newStart // Fixed: Property name matches DTO
            };

            await bookingService.UpdateBookingAsync(dto, userId);
            TempData["SuccessMessage"] = "Booking rescheduled successfully.";
            
            if (User.IsInRole(RoleConstants.Provider))
            {
                return RedirectToAction(nameof(Received));
            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reschedule booking {BookingId}", model.BookingId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}