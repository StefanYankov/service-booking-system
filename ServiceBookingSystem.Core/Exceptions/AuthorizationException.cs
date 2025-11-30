namespace ServiceBookingSystem.Core.Exceptions;

/// <summary>
/// Represents an error that occurs when a user attempts to perform an action
/// for which they do not have sufficient permissions.
/// This typically translates to an HTTP 403 Forbidden response.
/// </summary>
public class AuthorizationException : AppException
{
    public string UserId { get; }
    public string Action { get; }

    public AuthorizationException(string userId, string action)
        : base($"User '{userId}' is not authorized to perform action '{action}'.")
    {
        UserId = userId;
        Action = action;
    }
}