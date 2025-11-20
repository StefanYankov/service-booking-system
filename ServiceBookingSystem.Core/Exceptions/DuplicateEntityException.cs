namespace ServiceBookingSystem.Core.Exceptions;

/// <summary>
/// Represents an error that occurs when an attempt is made to create an entity
/// that would violate a uniqueness constraint (e.g., creating a category with a name that already exists).
/// </summary>
public class DuplicateEntityException : AppException
{
    public string? EntityName { get; }

    public object? DuplicateValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a default message.
    /// </summary>
    public DuplicateEntityException() 
        : base("A duplicate entity was found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a specific error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DuplicateEntityException(string message) 
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a formatted error message,
    /// capturing the details of the duplication.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="duplicateValue">The value that already exists.</param>
    public DuplicateEntityException(string entityName, object duplicateValue)
        : base($"A {entityName} with the value '{duplicateValue}' already exists.")
    {
        EntityName = entityName;
        DuplicateValue = duplicateValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a specific error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DuplicateEntityException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
