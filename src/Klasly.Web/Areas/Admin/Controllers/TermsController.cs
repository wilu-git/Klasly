using Klasly.Web.Infrastructure;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public class TermsController(ITermService terms) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await terms.ListAsync(ct));

    public IActionResult Create() => View("Form", new TermFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(TermFormViewModel model, CancellationToken ct) =>
        SaveAsync(model, () => terms.CreateAsync(model, ct));

    public async Task<IActionResult> Edit(int id, CancellationToken ct) => View("Form", await terms.GetFormAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, TermFormViewModel model, CancellationToken ct)
    {
        model.Id = id;
        return SaveAsync(model, () => terms.UpdateAsync(model, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var term = await terms.ActivateAsync(id, ct);
        TempData["Success"] = $"{term.Name} is now the active term.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await terms.DeactivateAsync(id, ct);
        TempData["Success"] = "Term deactivated. No term is active until you make one active.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(TermFormViewModel model, Func<Task> save)
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

        TempData["Success"] = $"Term {model.Name} saved.";
        return RedirectToAction(nameof(Index));
    }
}
