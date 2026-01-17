using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Web.Models;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(ValidationConstraints.User.NameMaxLength, MinimumLength = ValidationConstraints.User.NameMinLength)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(ValidationConstraints.User.NameMaxLength, MinimumLength = ValidationConstraints.User.NameMinLength)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
    
    public IList<string> Roles { get; set; } = new List<string>();
}