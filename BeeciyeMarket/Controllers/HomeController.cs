using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BeeciyeMarket.Data;
using BeeciyeMarket.Models;

namespace BeeciyeMarket.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Product");
        }

        if (User.IsInRole(Roles.Admin))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        return RedirectToAction("Dashboard", "Seller");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
