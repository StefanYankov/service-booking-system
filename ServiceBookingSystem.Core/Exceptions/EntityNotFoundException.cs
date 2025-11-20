namespace ServiceBookingSystem.Core.Exceptions;


/// <summary>
/// Represents an error that occurs when a requested entity is not found in the data store.
/// </summary>
public class EntityNotFoundException : AppException
{
    /// <summary>
    /// Gets the name of the entity that was not found.
    /// </summary>
    public string EntityName { get; }

    /// <summary>
    /// Gets the key or identifier that was used to search for the entity.
    /// </summary>
    public object EntityKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a formatted error message.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="entityKey">The key that was not found.</param>
    public EntityNotFoundException(string entityName, object entityKey)
        : base($"Entity '{entityName}' with key '{entityKey}' was not found.")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }
}