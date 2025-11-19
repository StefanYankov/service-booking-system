namespace ServiceBookingSystem.Core.Exceptions;

/// <summary>
/// Represents the base class for all custom exceptions in the application.
/// This class serves as a common marker for exceptions that originate from the application's
/// business logic, allowing for centralized exception handling policies.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected AppException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    protected AppException(string message, Exception innerException) : base(message, innerException)
    {
    }
}