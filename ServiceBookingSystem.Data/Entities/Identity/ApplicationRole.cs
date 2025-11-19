using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Identity;

/// <summary>
/// Represents a role in the application, extending the base IdentityRole with audit information
/// and a description. This allows for tracking when a role is created or modified and provides
/// a human-readable description for administrative purposes.
/// </summary>
public class ApplicationRole : IdentityRole, IAuditInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationRole"/> class.
    /// </summary>
    public ApplicationRole()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationRole"/> class with the specified role name.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    public ApplicationRole(string roleName)
        : base(roleName)
    {
    }
    
    /// <summary>
    /// Gets or sets the description for this role, providing context for its purpose.
    /// </summary>
    [StringLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when the role was created.
    /// This is automatically set by the ApplicationDbContext upon creation.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when the role was last modified.
    /// This is automatically set by the ApplicationDbContext upon modification.
    /// </summary>
    public DateTime? ModifiedOn { get; set; }
}
