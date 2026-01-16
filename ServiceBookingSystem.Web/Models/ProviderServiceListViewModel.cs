namespace ServiceBookingSystem.Web.Models;

public class ProviderServiceListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}