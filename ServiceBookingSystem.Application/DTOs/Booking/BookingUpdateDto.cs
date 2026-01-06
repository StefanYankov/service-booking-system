using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Booking;

public class BookingUpdateDto
{
    [Required]
    public string Id { get; set; } = null!;

    [Required]
    public DateTime BookingStart { get; set; }

    [StringLength(ValidationConstraints.Booking.NotesMaxLength)]
    public string? Notes { get; set; }
}