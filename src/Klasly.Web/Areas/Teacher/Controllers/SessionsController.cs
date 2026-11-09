using Klasly.Web.Infrastructure;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Teacher.Controllers;

/// <summary>Starting a session and its page. The API checks that the session belongs to this teacher.</summary>
[Area("Teacher"), Authorize(Roles = "Teacher")]
public class SessionsController(ISessionService sessions) : Controller
{
    public async Task<IActionResult> Details(int id, CancellationToken ct) => View(await sessions.GetSessionAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartSessionViewModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var (sessionId, openError) = await sessions.StartAsync(model, ct);
                if (openError is not null) TempData["Error"] = openError;
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }
            catch (ApiException ex) when (ex.IsFormError)
            {
                ModelState.AddApiErrors(ex, model);
            }
        }

        // Show the class page again with the teacher's values and the error messages.
        var page = await sessions.GetClassAsync(model.ClassId, ct);
        return View("~/Areas/Teacher/Views/Classes/Details.cshtml", page with { Start = model });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Open(int id, CancellationToken ct) =>
        ChangeAsync(id, () => sessions.OpenAsync(id, ct), "Session opened. Students can scan now.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Close(int id, CancellationToken ct) =>
        ChangeAsync(id, () => sessions.CloseAsync(id, ct), "Session ended.");

    // 400/409 (e.g. already closed in another tab) → the API's message on the same page.
    private async Task<IActionResult> ChangeAsync(int id, Func<Task> change, string success)
    {
        try
        {
            await change();
            TempData["Success"] = success;
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
