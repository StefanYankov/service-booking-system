namespace ServiceBookingSystem.Application.DTOs.Service;

/// <summary>
/// A Data Transfer Object representing a service view for administrators.
/// Includes additional metadata like deletion status and provider email.
/// </summary>
public class ServiceAdminViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationInMinutes { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedOn { get; set; }
}
