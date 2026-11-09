using Klasly.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Teacher.Controllers;

/// <summary>The teacher's own classes. The API only returns classes this teacher teaches.</summary>
[Area("Teacher"), Authorize(Roles = "Teacher")]
public class ClassesController(ISessionService sessions) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await sessions.GetMyClassesAsync(ct));
}
