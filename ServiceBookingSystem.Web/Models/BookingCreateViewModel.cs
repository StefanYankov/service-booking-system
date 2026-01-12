using System.ComponentModel.DataAnnotations;

namespace ServiceBookingSystem.Web.Models;

public class BookingCreateViewModel
{
    [Required]
    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    [Display(Name = "Time Slot")]
    public TimeSpan Time { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes for Provider")]
    public string? Notes { get; set; }
    
    // Helper to combine for the Controller
    public DateTime GetBookingStart() => Date.Date.Add(Time);
}