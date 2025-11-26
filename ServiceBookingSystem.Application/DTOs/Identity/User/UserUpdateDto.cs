using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Identity.User;

/// <summary>
/// DTO for updating a user's profile information.
/// This operation is intended for administrators.
/// </summary>
public class UserUpdateDto
{
    [Required]
    public required string Id { get; init; }

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
}
