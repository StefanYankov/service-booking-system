using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace ServiceBookingSystem.IntegrationTests;

public class MockAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
    {
        return new AntiforgeryTokenSet("test-token", "test-cookie", "test-form", "test-header");
    }

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
    {
        return new AntiforgeryTokenSet("test-token", "test-cookie", "test-form", "test-header");
    }

    public Task<bool> IsRequestValidAsync(HttpContext httpContext)
    {
        return Task.FromResult(true);
    }

    public Task ValidateRequestAsync(HttpContext httpContext)
    {
        return Task.CompletedTask;
    }

    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
        // No-op
    }
}