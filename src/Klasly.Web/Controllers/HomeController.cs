using System.Diagnostics;
using Klasly.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Controllers;

public class HomeController : Controller
{
    /// <summary>Sends each role to its portal. Not logged in → the cookie handler redirects to the login page.</summary>
    [Authorize]
    public IActionResult Index() =>
        User.IsInRole("Admin") ? RedirectToAction("Index", "Home", new { area = "Admin" }) : View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
