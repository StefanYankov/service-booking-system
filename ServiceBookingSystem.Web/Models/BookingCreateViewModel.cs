using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Web.Models;

public class BookingCreateViewModel : ITimeSlotPickerModel
{
    [Required]
    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Required]
    public TimeSpan Time { get; set; }

    [StringLength(ValidationConstraints.Booking.NotesMaxLength)]
    public string? Notes { get; set; }

    public DateTime GetBookingStart()
    {
        return Date.Date.Add(Time);
    }
}