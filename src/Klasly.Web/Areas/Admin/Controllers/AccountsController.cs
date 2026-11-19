using Klasly.Web.Infrastructure;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public class AccountsController(IAccountService accounts) : Controller
{
    public static readonly string[] Roles = ["Admin", "Teacher", "Student", "Parent"];

    public async Task<IActionResult> Index(string? role, CancellationToken ct)
    {
        if (role is not null && !Roles.Contains(role)) return RedirectToAction(nameof(Index));

        ViewData["Role"] = role;
        return View(await accounts.ListAsync(role, ct));
    }

    public IActionResult Create(string? role)
    {
        if (role is null || !Roles.Contains(role)) return RedirectToAction(nameof(Index));

        return View("Form", new AccountFormViewModel { Role = role });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountFormViewModel model, CancellationToken ct)
    {
        if (!Roles.Contains(model.Role)) return RedirectToAction(nameof(Index));
        // Checked here (not with [Required]) because the same view model is used by Edit, where it's absent.
        if (string.IsNullOrEmpty(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Enter a password (at least 8 characters).");
        if (!ModelState.IsValid) return View("Form", model);

        try
        {
            await accounts.CreateAsync(model, ct);
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            ModelState.AddApiErrors(ex, model);
            return View("Form", model);
        }

        TempData["Success"] = $"{model.Role} account for {model.FullName} created.";
        return RedirectToAction(nameof(Index), new { role = model.Role });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct) => View("Form", await accounts.GetFormAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountFormViewModel model, CancellationToken ct)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await accounts.UpdateAsync(model, ct);
                TempData["Success"] = $"Account for {model.FullName} saved.";
                return RedirectToAction(nameof(Index), new { role = model.Role });
            }
            catch (ApiException ex) when (ex.IsFormError)
            {
                ModelState.AddApiErrors(ex, model);
            }
        }

        // The links section isn't posted back, so reload it before showing the form again.
        if (model.Role == "Parent") model.ParentLinks = await accounts.GetParentLinksAsync(id, ct);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, string? role, CancellationToken ct)
    {
        try
        {
            await accounts.DeactivateAsync(id, ct);
            TempData["Success"] = "Account deactivated. That user can no longer log in.";
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            TempData["Error"] = ex.Message; // e.g. deactivating your own account
        }

        return RedirectToAction(nameof(Index), new { role });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDevice(int id, string? role, CancellationToken ct)
    {
        await accounts.ResetDeviceAsync(id, ct);
        TempData["Success"] = "Phone reset. The student's next login on the app registers their new phone.";
        return RedirectToAction(nameof(Index), new { role });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkStudent(int id, int studentId, string? relationship, bool receivesAlerts, CancellationToken ct)
    {
        if (studentId <= 0)
        {
            TempData["Error"] = "Pick a student to link.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        try
        {
            await accounts.LinkStudentAsync(id, studentId, string.IsNullOrWhiteSpace(relationship) ? null : relationship.Trim(), receivesAlerts, ct);
            TempData["Success"] = "Student linked.";
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkStudent(int id, int studentId, CancellationToken ct)
    {
        await accounts.UnlinkStudentAsync(id, studentId, ct);
        TempData["Success"] = "Student unlinked.";
        return RedirectToAction(nameof(Edit), new { id });
    }
}
