using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Domain;

/// <summary>
/// Represents a category for grouping services.
/// For example: "Music Lessons", "Home Repair", "Tutoring".
/// Inherits from DeletableEntity to support auditing and soft deletion.
/// </summary>
public class Category : DeletableEntity<int>
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new HashSet<Service>();
}
