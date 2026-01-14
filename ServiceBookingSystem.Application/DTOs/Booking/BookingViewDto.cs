namespace ServiceBookingSystem.Application.DTOs.Booking;

public class BookingViewDto
{
    public string Id { get; set; } = null!;
    
    public int ServiceId { get; set; }
    
    public string ServiceName { get; set; } = null!;
    
    public decimal ServicePrice { get; set; }
    
    public string CustomerId { get; set; } = null!;
    
    public string CustomerName { get; set; } = null!;
    
    public string? CustomerEmail { get; set; }
    
    public string? CustomerPhone { get; set; }
    
    public string ProviderId { get; set; } = null!;
    
    public string ProviderName { get; set; } = null!;

    public DateTime BookingStart { get; set; }
    
    public string Status { get; set; } = null!;
    
    public string? Notes { get; set; }
    
    public DateTime CreatedOn { get; set; }
}