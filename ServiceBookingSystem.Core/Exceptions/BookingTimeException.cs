namespace ServiceBookingSystem.Core.Exceptions;

/// <summary>
/// Represents an error that occurs when an operation is attempted at an invalid time
/// (e.g., completing a booking before it has started).
/// </summary>
public class BookingTimeException : AppException
{
    public string BookingId { get; }
    public DateTime BookingTime { get; }

    public BookingTimeException(string bookingId, DateTime bookingTime, string message)
        : base(message)
    {
        BookingId = bookingId;
        BookingTime = bookingTime;
    }
}