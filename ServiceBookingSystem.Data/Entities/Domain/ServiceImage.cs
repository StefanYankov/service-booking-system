using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Domain;

/// <summary>
/// Represents a single image in the gallery for a service.
/// This entity is auditable but not soft-deletable. When an image is removed, it is hard-deleted.
/// </summary>
public class ServiceImage : AuditableEntity<int>
{
    /// <summary>
    /// Gets or sets the URL pointing to the stored image.
    /// This could be a local path or a URL to a cloud storage service like Azure Blob Storage.
    /// </summary>
    [Required]
    [StringLength(2048)]
    public required string ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the main thumbnail image for the service.
    /// It is recommended that only one image per service has this flag set to true.
    /// </summary>
    public bool IsThumbnail { get; set; }

    // --- Foreign Keys and Navigation Properties ---

    public int ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public virtual Service Service { get; set; } = null!;
}
