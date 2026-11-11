using Klasly.Web.Models.Api;

namespace Klasly.Web.Repositories.Interfaces;

/// <summary>The admin's read-only view of the API's Notifications outbox.</summary>
public interface INotificationRepository
{
    Task<List<FailedEmailResponse>> ListFailedEmailsAsync(CancellationToken ct);
}
