using ServiceBookingSystem.Application.DTOs.Shared;

namespace ServiceBookingSystem.Application.DTOs.Service;

/// <summary>
/// Represents the criteria used to search and filter services.
/// Inherits pagination and sorting capabilities.
/// </summary>
public class ServiceSearchParameters : PagingAndSortingParameters
{
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsOnline { get; set; }
}