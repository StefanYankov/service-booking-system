namespace ServiceBookingSystem.Application.DTOs.Shared;

public class UserQueryParameters : PagingAndSortingParameters
{
    /// <summary>
    /// A search term to filter users by.
    /// The search will be applied to fields like FirstName, LastName, and Email.
    /// </summary>
    public string? SearchTerm { get; set; }
}