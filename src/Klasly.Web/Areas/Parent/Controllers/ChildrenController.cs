using Klasly.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Parent.Controllers;

/// <summary>The parent's linked children. The API returns only this parent's children (403 for any other).</summary>
[Area("Parent"), Authorize(Roles = "Parent")]
public class ChildrenController(IParentService parents) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var children = await parents.GetChildrenAsync(ct);
        return children.Count == 1 ? RedirectToAction(nameof(Details), new { id = children[0].StudentId }) : View(children);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct) => View(await parents.GetChildAsync(id, ct));
}
