using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;

namespace ServiceBookingSystem.Web.Models;

public class ServiceListViewModel
{
    public PagedResult<ServiceViewDto> Services { get; set; } = new(new List<ServiceViewDto>(), 0, 1, 10);
    public ServiceSearchParameters SearchParams { get; set; } = new();
    
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Cities { get; set; } = new();
}