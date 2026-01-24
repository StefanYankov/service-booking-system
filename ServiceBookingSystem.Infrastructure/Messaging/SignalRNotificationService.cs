using Microsoft.AspNetCore.SignalR;
using ServiceBookingSystem.Application.Interfaces.Infrastructure;
using ServiceBookingSystem.Infrastructure.Hubs;

namespace ServiceBookingSystem.Infrastructure.Messaging;

public class SignalRNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<NotificationHub> hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    public async Task SendToUserAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", message, cancellationToken);
    }
}