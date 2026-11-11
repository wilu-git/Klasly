using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>The Notifications outbox, read-only, for the admin (guide §27.8: failed emails go to admin review).</summary>
[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "AdminOnly")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>The newest 100 parent emails that failed for good, with the last error.</summary>
    [HttpGet("failed")]
    public Task<List<FailedEmailResponse>> Failed(CancellationToken ct) => notificationService.ListFailedEmailsAsync(ct);
}
