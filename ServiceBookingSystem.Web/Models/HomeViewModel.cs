using Microsoft.AspNetCore.Mvc.Rendering;

namespace ServiceBookingSystem.Web.Models;

public class HomeViewModel
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? City { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Cities { get; set; } = new();
}