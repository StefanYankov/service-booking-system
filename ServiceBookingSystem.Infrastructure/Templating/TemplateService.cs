using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ServiceBookingSystem.Application.Interfaces;

namespace ServiceBookingSystem.Infrastructure.Templating;

/// <summary>
/// A simple template service that reads HTML files from an embedded resource and replaces placeholders.
/// </summary>
public class TemplateService : ITemplateService
{
    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> model)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"ServiceBookingSystem.Infrastructure.Messaging.Templates.{templateName}";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Template '{templateName}' not found as an embedded resource.");
        }

        using var reader = new StreamReader(stream);
        var templateContent = await reader.ReadToEndAsync();

        foreach (var entry in model)
        {
            templateContent = templateContent.Replace($"{{{{{entry.Key}}}}}", entry.Value);
        }

        return templateContent;
    }
}
