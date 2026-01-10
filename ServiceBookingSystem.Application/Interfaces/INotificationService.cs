using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Orchestrates the sending of business notifications (emails) for system events.
/// Decouples the core business logic (e.g., BookingService) from the details of email templating and sending.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends notifications to both the Customer (Confirmation of receipt) and Provider (New Request)
    /// when a new booking is created.
    /// </summary>
    Task NotifyBookingCreatedAsync(Booking booking);

    /// <summary>
    /// Sends a notification to the Customer that their booking has been confirmed.
    /// </summary>
    Task NotifyBookingConfirmedAsync(Booking booking);

    /// <summary>
    /// Sends a notification to the Customer that their booking has been declined.
    /// </summary>
    Task NotifyBookingDeclinedAsync(Booking booking);

    /// <summary>
    /// Sends a notification to the other party that the booking has been cancelled.
    /// </summary>
    /// <param name="booking">The booking details.</param>
    /// <param name="cancelledByProvider">True if provider cancelled (notify customer), False if customer cancelled (notify provider).</param>
    Task NotifyBookingCancelledAsync(Booking booking, bool cancelledByProvider);

    /// <summary>
    /// Sends a notification to the Provider that a booking has been rescheduled by the Customer.
    /// </summary>
    /// <param name="booking">The updated booking.</param>
    /// <param name="oldDate">The original booking date.</param>
    Task NotifyBookingRescheduledAsync(Booking booking, DateTime oldDate);
}