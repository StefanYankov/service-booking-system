using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;

namespace ServiceBookingSystem.Web.Models;

public class ServiceDetailsViewModel
{
    public ServiceViewDto Service { get; set; } = null!;
    public PagedResult<ReviewViewDto> Reviews { get; set; } = null!;
}