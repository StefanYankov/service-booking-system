namespace ServiceBookingSystem.Application.DTOs.Shared;

public class PagingAndSortingParameters : PagingParameters
{
    public string? SortBy { get; set; } = "Id"; 
    public string? SortDirection { get; set; } = "asc";
}