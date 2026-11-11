using Klasly.Web.Infrastructure;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Teacher.Controllers;

/// <summary>Adding and editing assessments, and the grade sheet. The API checks the class is this teacher's.</summary>
[Area("Teacher"), Authorize(Roles = "Teacher")]
public class AssessmentsController(IAssessmentService assessments, ISessionService sessions) : Controller
{
    public async Task<IActionResult> Details(int id, CancellationToken ct) => View(await assessments.GetPageAsync(id, ct));

    /// <summary>The Add assessment form on the class page; success goes straight to the new grade sheet.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssessmentFormViewModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var created = await assessments.CreateAsync(model, ct);
                TempData["Success"] = $"{created.Title} added. Enter the scores below.";
                return RedirectToAction(nameof(Details), new { id = created.AssessmentId });
            }
            catch (ApiException ex) when (ex.IsFormError)
            {
                AddFormErrors(ex, model);
            }
        }

        // Show the class page again with the teacher's values and the messages next to the inputs.
        var page = await sessions.GetClassAsync(model.ClassId, ct);
        return View("~/Areas/Teacher/Views/Classes/Details.cshtml", page with { NewAssessment = model });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssessmentFormViewModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var updated = await assessments.UpdateAsync(id, model, ct);
                TempData["Success"] = $"{updated.Title} updated.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ApiException ex) when (ex.IsFormError)
            {
                AddFormErrors(ex, model);
            }
        }

        var page = await assessments.GetPageAsync(id, ct);
        return View(nameof(Details), page with { Edit = model });
    }

    /// <summary>The whole grade sheet in one post. Row errors from the API (grades[i].score) show on their rows.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGrades(int id, List<GradeInputViewModel> grades, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var saved = await assessments.SaveGradesAsync(id, grades, ct);
                TempData["Success"] = saved.Added + saved.Changed == 0
                    ? "No changes to save."
                    : $"Grades saved: {saved.Added} new, {saved.Changed} changed.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ApiException ex) when (ex.IsFormError)
            {
                // ModelState keys ignore case, so the API's "grades[1].score" lands on the input named "Grades[1].Score".
                foreach (var (key, messages) in ex.Errors)
                    foreach (var message in messages) ModelState.AddModelError(key, message);
                if (ex.Errors.Count == 0) ModelState.AddModelError("grades", ex.Message); // e.g. 409 race
            }
        }

        // Keep what the teacher typed, so fixing one row doesn't mean typing the others again.
        return View(nameof(Details), await assessments.GetPageAsync(id, ct, grades));
    }

    // A 409 (title already used) has no field in the API answer; it belongs next to Title, not in another form's summary.
    private void AddFormErrors(ApiException ex, AssessmentFormViewModel model)
    {
        if (ex.Errors.Count == 0 && ex.StatusCode == StatusCodes.Status409Conflict)
            ModelState.AddModelError(nameof(model.Title), ex.Message);
        else
            ModelState.AddApiErrors(ex, model);
    }
}
