using Microsoft.AspNetCore.Mvc;

namespace ServiceBookingSystem.Web.Controllers;

public class ServiceController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}