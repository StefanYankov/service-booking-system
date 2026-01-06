using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceBookingSystem.Core.Constants;
using ServiceBookingSystem.Data.Entities.Common;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Data.Entities.Domain;

public class Booking : DeletableEntity<string>
{
    public Booking()
    {
        this.Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// The specific date and time when the service is scheduled to begin.
    /// </summary>
    public DateTime BookingStart { get; set; }

    /// <summary>
    /// The current status of the booking lifecycle.
    /// </summary>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>
    /// </summary>
    [StringLength(ValidationConstraints.Booking.NotesMaxLength)]
    public string? Notes { get; set; }

    // --- Foreign Keys and Navigation Properties ---

    [Required]
    public int ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public virtual Service Service { get; set; } = null!;

    [Required]
    [StringLength(ValidationConstraints.User.IdMaxLength)]
    public required string CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual ApplicationUser Customer { get; set; } = null!;
}