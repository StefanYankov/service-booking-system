using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ServiceBookingSystem.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for handling real-time notifications.
/// Clients connect to this hub to receive updates.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    // We can add methods here for clients to call server, e.g., SendMessage.
    // For now, we only use it to push notifications from server to client (via IHubContext).
    
    public override async Task OnConnectedAsync()
    {
        // We can map connection IDs to User IDs here if needed, 
        // but SignalR's default UserProvider handles IPrincipal.Identity.Name mapping automatically.
        await base.OnConnectedAsync();
    }
}
