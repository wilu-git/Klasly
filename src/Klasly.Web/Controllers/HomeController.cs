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
        User.IsInRole("Admin") ? RedirectToAction("Index", "Home", new { area = "Admin" })
        : User.IsInRole("Teacher") ? RedirectToAction("Index", "Classes", new { area = "Teacher" })
        : User.IsInRole("Parent") ? RedirectToAction("Index", "Children", new { area = "Parent" })
        : View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Re-executed by <c>UseStatusCodePagesWithReExecute</c> for an error status with no body:
    /// 404 → styled Not found page, anything else → the Error page. The response keeps the original status code.
    /// (Method name differs from the route because <c>ControllerBase.StatusCode(int)</c> already exists.)
    /// </summary>
    [AllowAnonymous]
    [ActionName("StatusCode")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code)
    {
        var status = code is >= 400 and <= 599 ? code : StatusCodes.Status404NotFound;
        var result = status == StatusCodes.Status404NotFound
            ? View("NotFound")
            : View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        result.StatusCode = status;
        return result;
    }
}
