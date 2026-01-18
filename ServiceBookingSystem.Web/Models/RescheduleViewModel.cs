using System.ComponentModel.DataAnnotations;

namespace ServiceBookingSystem.Web.Models;

public class RescheduleViewModel : ITimeSlotPickerModel
{
    [Required]
    public string BookingId { get; set; } = string.Empty;

    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    
    public DateTime CurrentStart { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Required]
    public TimeSpan Time { get; set; }
}