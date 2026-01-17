using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceBookingSystem.IntegrationTests;

/// <summary>
/// A custom Authentication Handler for Integration Tests.
/// </summary>
/// <remarks>
/// <b>Why is this needed?</b>
/// <br/>
/// Testing MVC Controllers that use [Authorize] usually requires a valid User Principal.
/// Simulating a full browser login flow (GET Login -> POST Login -> Cookie) in integration tests is slow, flaky, and complex
/// (dealing with AntiForgery tokens, Cookies, Redirects).
/// <br/>
/// <b>How it works:</b>
/// <br/>
/// 1. This handler is registered in the Test Server's DI container (via WebApplicationFactory.WithWebHostBuilder).
/// 2. It intercepts requests and looks for a custom header (e.g., "X-Test-UserId").
/// 3. If found, it creates a ClaimsPrincipal for that User ID and assigns it to HttpContext.User.
/// 4. The Controller then sees the user as "Authenticated" without needing a real login or cookie.
/// <br/>
/// <b>Usage:</b>
/// <br/>
/// <code>
/// client.DefaultRequestHeaders.Add("X-Test-UserId", "user-guid-123");
/// client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator"); // Optional
/// var response = await client.GetAsync("/Protected/Route");
/// </code>
/// </remarks>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for the custom header
        if (!Request.Headers.TryGetValue("X-Test-UserId", out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("No User ID header found. Authentication failed."));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "Test User")
        };

        // Check for Role header
        if (Request.Headers.TryGetValue("X-Test-Role", out var role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }
        else
        {
            // Default roles if not specified (backward compatibility)
            claims.Add(new Claim(ClaimTypes.Role, "Customer"));
            claims.Add(new Claim(ClaimTypes.Role, "Provider"));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}