using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Identity;

/// <summary>
/// Represents a user in the application, extending the default IdentityUser with custom properties.
/// </summary>
public class ApplicationUser : IdentityUser, IAuditInfo
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string LastName { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
