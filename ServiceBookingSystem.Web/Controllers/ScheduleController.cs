using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Availability;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

[Authorize(Roles = RoleConstants.Provider)]
public class ScheduleController : Controller
{
    private readonly IAvailabilityService availabilityService;
    private readonly IServiceService serviceService;
    private readonly ILogger<ScheduleController> logger;

    public ScheduleController(
        IAvailabilityService availabilityService,
        IServiceService serviceService,
        ILogger<ScheduleController> logger)
    {
        this.availabilityService = availabilityService;
        this.serviceService = serviceService;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int serviceId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var service = await serviceService.GetServiceByIdAsync(serviceId);
        if (service == null) return NotFound();
        if (service.ProviderId != userId) return Forbid();

        var scheduleDto = await availabilityService.GetWeeklyScheduleAsync(serviceId);

        var model = new ScheduleViewModel
        {
            ServiceId = serviceId,
            ServiceName = service.Name,
            Days = scheduleDto.Days.Select(d => new DayScheduleViewModel
            {
                DayOfWeek = d.DayOfWeek,
                IsClosed = d.IsClosed,
                Segments = d.Segments.Select(s => new TimeSegmentViewModel
                {
                    Start = s.Start.ToTimeSpan(),
                    End = s.End.ToTimeSpan()
                }).ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWeekly(ScheduleViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            var dto = new WeeklyScheduleDto
            {
                Days = model.Days.Select(d => new DayScheduleDto
                {
                    DayOfWeek = d.DayOfWeek,
                    IsClosed = d.IsClosed,
                    Segments = d.Segments.Select(s => new TimeSegmentDto
                    {
                        Start = TimeOnly.FromTimeSpan(s.Start),
                        End = TimeOnly.FromTimeSpan(s.End)
                    }).ToList()
                }).ToList()
            };

            await availabilityService.UpdateWeeklyScheduleAsync(model.ServiceId, dto, userId);
            TempData["SuccessMessage"] = "Weekly schedule updated successfully.";
            return RedirectToAction(nameof(Index), new { serviceId = model.ServiceId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update schedule for Service {ServiceId}", model.ServiceId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Overrides(int serviceId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var service = await serviceService.GetServiceByIdAsync(serviceId);
        if (service == null) return NotFound();
        if (service.ProviderId != userId) return Forbid();

        var overrides = await availabilityService.GetOverridesAsync(serviceId);

        var model = new ScheduleOverrideListViewModel
        {
            ServiceId = serviceId,
            ServiceName = service.Name,
            Overrides = overrides.Select(o => new ScheduleOverrideViewModel
            {
                Id = o.Id,
                Date = o.Date,
                IsDayOff = o.IsDayOff,
                Segments = o.Segments.Select(s => new TimeSegmentViewModel
                {
                    Start = s.Start.ToTimeSpan(),
                    End = s.End.ToTimeSpan()
                }).ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOverride(CreateOverrideViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var dto = new ScheduleOverrideDto
            {
                Date = model.Date,
                IsDayOff = model.IsDayOff,
                Segments = new List<TimeSegmentDto>()
            };

            if (!model.IsDayOff && model.StartTime.HasValue && model.EndTime.HasValue)
            {
                dto.Segments.Add(new TimeSegmentDto
                {
                    Start = TimeOnly.FromTimeSpan(model.StartTime.Value),
                    End = TimeOnly.FromTimeSpan(model.EndTime.Value)
                });
            }

            await availabilityService.AddOverrideAsync(model.ServiceId, dto, userId);
            TempData["SuccessMessage"] = "Override added successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add override for Service {ServiceId}", model.ServiceId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Overrides), new { serviceId = model.ServiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOverride(int id, int serviceId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            await availabilityService.DeleteOverrideAsync(id, userId);
            TempData["SuccessMessage"] = "Override deleted successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete override {OverrideId}", id);
            TempData["ErrorMessage"] = "Failed to delete override.";
        }

        return RedirectToAction(nameof(Overrides), new { serviceId = serviceId });
    }
}
