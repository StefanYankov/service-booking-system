namespace ServiceBookingSystem.Application.Interfaces.Infrastructure;

/// <summary>
/// Defines a contract for sending real-time notifications to users (e.g., via SignalR).
/// This abstraction keeps the Application layer decoupled from specific WebSocket implementations.
/// </summary>
public interface IRealTimeNotificationService
{
    /// <summary>
    /// Sends a notification message to a specific user.
    /// </summary>
    /// <param name="userId">The ID of the recipient user.</param>
    /// <param name="message">The message content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendToUserAsync(string userId, string message, CancellationToken cancellationToken = default);
}