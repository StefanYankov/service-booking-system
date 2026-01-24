using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ServiceBookingSystem.Infrastructure.Hubs;

/// <summary>
/// Custom User ID Provider for SignalR.
/// Ensures that the User ID used for targeting connections matches the ClaimTypes.NameIdentifier (User.Id).
/// </summary>
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}