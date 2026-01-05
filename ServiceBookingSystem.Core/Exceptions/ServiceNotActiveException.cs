namespace ServiceBookingSystem.Core.Exceptions;

/// <summary>
/// Represents an error that occurs when an operation is attempted on a service that is currently inactive.
/// </summary>
public class ServiceNotActiveException : AppException
{
    public int ServiceId { get; }
    public string ServiceName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceNotActiveException"/> class.
    /// </summary>
    /// <param name="serviceId">The unique identifier of the service.</param>
    /// <param name="serviceName">The name of the service.</param>
    public ServiceNotActiveException(int serviceId, string serviceName)
        : base($"Service '{serviceName}' (ID: {serviceId}) is currently not active and cannot be booked.")
    {
        ServiceId = serviceId;
        ServiceName = serviceName;
    }
}