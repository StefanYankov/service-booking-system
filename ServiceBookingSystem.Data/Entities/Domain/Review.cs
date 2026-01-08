using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceBookingSystem.Data.Entities.Common;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Data.Entities.Domain;

/// <summary>
/// Represents a review and rating left by a customer for a specific service.
/// Inherits from DeletableEntity to support auditing and soft deletion.
/// </summary>
public class Review : DeletableEntity<int>
{
    /// <summary>
    /// Gets or sets the rating value, e.g., on a scale of 1 to 5.
    /// </summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }

    // --- Foreign Keys and Navigation Properties ---

    public int ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public virtual Service Service { get; set; } = null!;

    [StringLength(450)]
    public required string CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual ApplicationUser Customer { get; set; } = null!;
    
    [StringLength(450)]
    public required string BookingId { get; set; }

    [ForeignKey(nameof(BookingId))]
    public virtual Booking Booking { get; set; } = null!;
}
