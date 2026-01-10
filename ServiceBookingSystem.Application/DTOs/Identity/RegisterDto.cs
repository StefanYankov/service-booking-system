using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Identity;

public class RegisterDto
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

    [Required]
    [StringLength(ValidationConstraints.User.PasswordMaxLength, MinimumLength = ValidationConstraints.User.PasswordMinLength)]
    public required string Password { get; init; }

    [Required]
    public required string Role { get; init; }
}