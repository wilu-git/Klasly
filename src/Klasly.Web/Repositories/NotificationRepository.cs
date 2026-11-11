using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;

namespace Klasly.Web.Repositories;

public class NotificationRepository(HttpClient http) : INotificationRepository
{
    // Newest first, at most 100 (API order). Admin only.
    public Task<List<FailedEmailResponse>> ListFailedEmailsAsync(CancellationToken ct) =>
        http.GetApiAsync<List<FailedEmailResponse>>("api/notifications/failed", ct);
}
