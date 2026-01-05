namespace ServiceBookingSystem.Core.Exceptions;

/// <summary>
/// Represents an error that occurs when a requested booking slot is no longer available
/// (e.g., due to a race condition or conflicting booking).
/// </summary>
public class SlotUnavailableException : AppException
{
    public int ServiceId { get; }
    public DateTime SlotStart { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotUnavailableException"/> class.
    /// </summary>
    /// <param name="serviceId">The ID of the service that was requested.</param>
    /// <param name="slotStart">The start time of the slot that is unavailable.</param>
    public SlotUnavailableException(int serviceId, DateTime slotStart)
        : base($"The time slot starting at '{slotStart}' for Service ID '{serviceId}' is not available.")
    {
        ServiceId = serviceId;
        SlotStart = slotStart;
    }
}