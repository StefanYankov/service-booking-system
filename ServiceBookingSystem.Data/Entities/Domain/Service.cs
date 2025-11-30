using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceBookingSystem.Data.Entities.Common;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Data.Entities.Domain;

/// <summary>
/// Represents a service offered by a provider on the platform.
/// This is a central entity in the application's domain.
/// Inherits from DeletableEntity to support auditing and soft deletion.
/// </summary>
public class Service : DeletableEntity<int>
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(4000)]
    public required string Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the service is delivered online.
    /// </summary>
    public bool IsOnline { get; set; }

    [StringLength(255)]
    public string? StreetAddress { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the service is active and bookable.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the duration of the service in minutes.
    /// </summary>
    public int DurationInMinutes { get; set; }

    // --- Foreign Keys and Navigation Properties ---

    /// <summary>
    /// Foreign key for the ApplicationUser who is the provider of this service.
    /// </summary>
    [StringLength(450)]
    public required string ProviderId { get; set; }

    // --- Foreign Keys and Navigation Properties ---
    
    /// <summary>
    /// Navigation property to the ApplicationUser who provides the service.
    /// </summary>
    [ForeignKey(nameof(ProviderId))]
    public virtual ApplicationUser Provider { get; set; } = null!;

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OperatingHour> OperatingHours { get; set; } = new HashSet<OperatingHour>();

    public virtual ICollection<ServiceImage> Images { get; set; } = new HashSet<ServiceImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new HashSet<Review>();
}
