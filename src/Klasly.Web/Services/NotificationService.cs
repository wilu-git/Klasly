using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;

namespace Klasly.Web.Services;

/// <summary>The admin's Failed emails page. No rules here: the API decides what counts as failed.</summary>
public class NotificationService(INotificationRepository notifications) : INotificationService
{
    public Task<List<FailedEmailResponse>> GetFailedEmailsAsync(CancellationToken ct) => notifications.ListFailedEmailsAsync(ct);
}
