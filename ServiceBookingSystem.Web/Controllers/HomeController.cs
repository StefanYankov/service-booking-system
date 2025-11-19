using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // --- Structured Logging Example ---
        // This demonstrates writing a structured log. The first argument is a message template.
        // The subsequent arguments are property values that will be captured in a structured format.
        // This is far more powerful than simple string interpolation.
        _logger.LogInformation("User is viewing the Home page at {ViewTime}", DateTime.UtcNow);
        // --- End Logging Example ---

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
