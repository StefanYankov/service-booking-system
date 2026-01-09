using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Defines the contract for a service responsible for generating authentication tokens.
/// This abstracts the JWT generation logic from the controllers.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT (JSON Web Token) for the specified user.
    /// The token will contain claims for the user's identity (ID, Email) and their roles.
    /// </summary>
    /// <param name="user">The application user for whom the token is generated.</param>
    /// <param name="roles">The list of roles assigned to the user.</param>
    /// <returns>A string representation of the signed JWT.</returns>
    string GenerateToken(ApplicationUser user, IList<string> roles);
}