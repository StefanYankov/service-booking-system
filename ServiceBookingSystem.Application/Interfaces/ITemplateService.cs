namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Defines a contract for a service that renders content from templates.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Renders a template with the provided model data.
    /// </summary>
    /// <param name="templateName">The name of the template file (e.g., "ConfirmEmail.html").</param>
    /// <param name="model">A dictionary containing the placeholder keys and their values.</param>
    /// <returns>The rendered string content of the template.</returns>
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> model);
}
