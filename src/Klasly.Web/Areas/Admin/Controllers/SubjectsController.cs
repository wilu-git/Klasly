using Klasly.Web.Infrastructure;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Admin.Controllers;

/// <summary>Subject catalog. No delete: the Subjects table has no column to soft-delete with (Milestone 4 decision 3).</summary>
[Area("Admin"), Authorize(Roles = "Admin")]
public class SubjectsController(ISubjectService subjects) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await subjects.ListAsync(ct));

    public IActionResult Create() => View("Form", new SubjectFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(SubjectFormViewModel model, CancellationToken ct) =>
        SaveAsync(model, () => subjects.CreateAsync(model, ct));

    public async Task<IActionResult> Edit(int id, CancellationToken ct) => View("Form", await subjects.GetFormAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, SubjectFormViewModel model, CancellationToken ct)
    {
        model.Id = id;
        return SaveAsync(model, () => subjects.UpdateAsync(model, ct));
    }

    private async Task<IActionResult> SaveAsync(SubjectFormViewModel model, Func<Task> save)
    {
        if (!ModelState.IsValid) return View("Form", model);

        try
        {
            await save();
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            ModelState.AddApiErrors(ex, model);
            return View("Form", model);
        }

        TempData["Success"] = $"Subject {model.Code} saved.";
        return RedirectToAction(nameof(Index));
    }
}
