using Klasly.Web.Infrastructure;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Admin.Controllers;

/// <summary>Classes, plus enrolling and dropping students on a class's Details page. No class delete (Milestone 4 decision 3).</summary>
[Area("Admin"), Authorize(Roles = "Admin")]
public class ClassesController(IClassService classes) : Controller
{
    public async Task<IActionResult> Index(int? termId, CancellationToken ct) => View(await classes.GetListAsync(termId, ct));

    public async Task<IActionResult> Create(CancellationToken ct) => View("Form", await classes.GetNewFormAsync(ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(ClassFormViewModel model, CancellationToken ct) =>
        SaveAsync(model, () => classes.CreateAsync(model, ct), ct);

    public async Task<IActionResult> Edit(int id, CancellationToken ct) => View("Form", await classes.GetEditFormAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, ClassFormViewModel model, CancellationToken ct)
    {
        model.Id = id;
        return SaveAsync(model, () => classes.UpdateAsync(model, ct), ct);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct) => View(await classes.GetDetailsAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int id, int? studentId, CancellationToken ct)
    {
        if (studentId is null or <= 0)
        {
            TempData["Error"] = "Pick a student to enroll.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await classes.EnrollAsync(id, studentId.Value, ct);
            TempData["Success"] = "Student enrolled.";
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Drop(int id, int enrollmentId, CancellationToken ct)
    {
        await classes.DropAsync(enrollmentId, ct);
        TempData["Success"] = "Student dropped. Their attendance and grades are kept.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> SaveAsync(ClassFormViewModel model, Func<Task> save, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await save();
                TempData["Success"] = $"Class section {model.Section} saved.";
                return RedirectToAction(nameof(Index), new { termId = model.TermId });
            }
            catch (ApiException ex) when (ex.IsFormError)
            {
                ModelState.AddApiErrors(ex, model);
            }
        }

        // Dropdown options aren't posted back, so rebuild them before showing the form again.
        await classes.FillOptionsAsync(model, ct);
        return View("Form", model);
    }
}
