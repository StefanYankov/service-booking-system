namespace ServiceBookingSystem.Application.DTOs.Service;

public class ServiceViewDto
{
    public int Id { get; set; }
    
    public required string Name { get; set; }

    public required string Description { get; set; }

    public decimal Price { get; set; }

    public int DurationInMinutes { get; set; }

    public int CategoryId { get; set; }
    
    public required string CategoryName { get; set; }

    public bool IsOnline { get; set; }

    public string? StreetAddress { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }
    
    public bool IsActive { get; set; }
    
    public required string ProviderId { get; set; }

    public required string ProviderName { get; set; }
    
    public string? MainImageUrl { get; set; }
    
    public double? AverageRating { get; set; }
    
    public int? TotalReviews { get; set; }
    
    public List<ServiceImageDto> Images { get; set; } = new();
}