using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Booking;

public class BookingCreateDto
{
    [Required]
    public int ServiceId { get; set; }

    [Required]
    public DateTime BookingStart { get; set; }

    [StringLength(ValidationConstraints.Booking.NotesMaxLength)]
    public string? Notes { get; set; }
}