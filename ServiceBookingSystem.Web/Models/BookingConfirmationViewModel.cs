namespace ServiceBookingSystem.Web.Models;

public class BookingConfirmationViewModel
{
    public string BookingId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime BookingStart { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Notes { get; set; }
}