using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Identity.User;

/// <summary>
/// DTO for creating a new user.
/// This operation is intended for administrators.
/// </summary>
public class UserCreateDto
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
    [StringLength(ValidationConstraints.User.PasswordMaxLength, MinimumLength = ValidationConstraints.User.PasswordMinLength)]
    // TODO: Add a Regex attribute here if password complexity rules are enabled in IdentityOptions.
    public required string Password { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = ErrorMessages.AtLeastOneRoleIsRequired)]
    public List<string> Roles { get; init; } = new();
}
