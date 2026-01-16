namespace ServiceBookingSystem.Application.DTOs.Service;

public class ServiceImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsThumbnail { get; set; }
}