using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Service;

public class ServiceUpdateDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(ValidationConstraints.Service.NameMaxLength, MinimumLength = ValidationConstraints.Service.NameMinLength)]
    public required string Name { get; set; }

    [StringLength(ValidationConstraints.Service.DescriptionMaxLength)]
    public required string Description { get; set; }

    [Range(0, (double)decimal.MaxValue)]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.AtLeastOneMinuteDuration)]
    public int DurationInMinutes { get; set; }

    [Required]
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the service is delivered online.
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Allow a provider to activate/deactivate their service
    /// </summary>
    public bool IsActive { get; set; }

    [StringLength(ValidationConstraints.Service.AddressStreetMaximumLength)]
    public string? StreetAddress { get; set; }

    [StringLength(ValidationConstraints.Service.AddressCityMaximumLength)]
    public string? City { get; set; }

    [StringLength(ValidationConstraints.Service.PostalCodeMaximumLength)]
    public string? PostalCode { get; set; }
}