using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Web.Models;

public class ServiceCreateViewModel
{
    [Required]
    [StringLength(ValidationConstraints.Service.NameMaxLength, MinimumLength = ValidationConstraints.Service.NameMinLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(ValidationConstraints.Service.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required]
    [Range(15, 480)]
    [Display(Name = "Duration (minutes)")]
    public int DurationInMinutes { get; set; } = 60;

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Is Online Service?")]
    public bool IsOnline { get; set; }

    [StringLength(ValidationConstraints.Service.AddressStreetMaximumLength)]
    [Display(Name = "Street Address")]
    public string? StreetAddress { get; set; }

    [StringLength(ValidationConstraints.Service.AddressCityMaximumLength)]
    public string? City { get; set; }

    [StringLength(ValidationConstraints.Service.PostalCodeMaximumLength)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }
    
    // For Dropdown
    public List<SelectListItem> Categories { get; set; } = new();
}