using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Identity.User;

public class UserUpdateDto
{
    [Required]
    [StringLength(ValidationConstraints.User.NameMaxLength, MinimumLength = ValidationConstraints.User.NameMinLength)]
    public required string FirstName { get; init; }
    
    [Required]
    [StringLength(ValidationConstraints.User.NameMaxLength, MinimumLength = ValidationConstraints.User.NameMinLength)]
    public required string LastName { get; init; }
    
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
    
    [Phone]
    public string? PhoneNumber { get; init; }
    
    [Required]
    // TODO: add regex if @services.AddIdentity<ApplicationUser, ApplicationRole>(options... has those requirements
    [StringLength(ValidationConstraints.User.PasswordMaxLength, MinimumLength = ValidationConstraints.User.PasswordMinLength)]
    public required string Password { get; init; }

    [Required]
    public required string Role { get; init; }
}