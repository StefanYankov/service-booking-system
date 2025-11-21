using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Identity;

/// <summary>
/// Represents a user in the application, extending the default IdentityUser with custom properties.
/// </summary>
public class ApplicationUser : IdentityUser, IAuditInfo
{
    // This parameterless constructor is required by:
    // - ASP.NET Core Identity stores (UserStore, RoleStore)
    // - Entity Framework Core (change tracking, lazy loading, migrations)
    // - JSON serializers (System.Text.Json when creating objects during deserialization)
    //
    // Without it, .NET 8+ throws: "cannot satisfy the 'new()' constraint because type has required members"
    // We use [SetsRequiredMembers] to suppress the warning while keeping 'required' keyword safety.
    [SetsRequiredMembers]
    public ApplicationUser()
    {
    }

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