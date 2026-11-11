using Klasly.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.Web.Areas.Admin.Controllers;

/// <summary>Parent emails Brevo refused for good or that ran out of retries (guide §27.8: admin review). Read-only.</summary>
[Area("Admin"), Authorize(Roles = "Admin")]
public class FailedEmailsController(INotificationService notifications) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await notifications.GetFailedEmailsAsync(ct));
}
