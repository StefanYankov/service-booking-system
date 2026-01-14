namespace ServiceBookingSystem.Web.Models;

public class CustomerBookingViewModel
{
    public string Id { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime BookingStart { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProviderBookingViewModel
{
    public string Id { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty; // For link
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime BookingStart { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class ProviderCustomerDetailsViewModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public List<ProviderBookingViewModel> Bookings { get; set; } = new();
}