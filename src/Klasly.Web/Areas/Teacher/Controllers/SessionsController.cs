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

    /// <summary>The QR + roster panel the session page polls every 5 s. API errors go through ApiExceptionFilter like any page.</summary>
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Live(int id, CancellationToken ct) => PartialView("_Live", await sessions.GetLiveAsync(id, ct));

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
                ModelState.MoveSummaryErrorsTo("start"); // shown only in _StartSession, not in the Add assessment form
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

    /// <summary>One row's status dropdown on an ended session. The API's 400/409 message shows on the same page.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Correct(int id, int studentId, string? status, string? currentStatus, CancellationToken ct)
    {
        try
        {
            var row = await sessions.CorrectAttendanceAsync(id, studentId, status, ct);
            // currentStatus is what the page showed; the API itself treats the same status as no change.
            TempData["Success"] = status == currentStatus
                ? $"No change: {row.FullName} is already {row.Status}."
                : $"Attendance updated for {row.FullName}.";
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            TempData["Error"] = ex.Message;
        }

        // Land back on the corrected student's row (roster rows have id="row-{studentId}").
        return RedirectToAction(nameof(Details), null, new { id }, $"row-{studentId}");
    }

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
