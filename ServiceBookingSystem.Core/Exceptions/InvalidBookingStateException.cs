namespace ServiceBookingSystem.Core.Exceptions;

/// <summary>
/// Represents an error that occurs when an action is attempted on a booking that is in an invalid state for that action.
/// For example, attempting to update or confirm a booking that has already been cancelled or completed.
/// </summary>
public class InvalidBookingStateException : AppException
{
    public string BookingId { get; }
    public string CurrentState { get; }
    public string Action { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidBookingStateException"/> class.
    /// </summary>
    /// <param name="bookingId">The unique identifier of the booking.</param>
    /// <param name="currentState">The current status of the booking (e.g., Cancelled, Completed).</param>
    /// <param name="action">The action that was attempted (e.g., Update, Confirm).</param>
    public InvalidBookingStateException(string bookingId, string currentState, string action)
        : base($"Cannot perform action '{action}' on Booking '{bookingId}' because it is in state '{currentState}'.")
    {
        BookingId = bookingId;
        CurrentState = currentState;
        Action = action;
    }
}