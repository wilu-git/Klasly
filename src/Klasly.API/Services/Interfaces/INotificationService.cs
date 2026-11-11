using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

/// <summary>Read side of the Notifications outbox for the admin.</summary>
public interface INotificationService
{
    /// <summary>The newest 100 parent emails that failed for good.</summary>
    Task<List<FailedEmailResponse>> ListFailedEmailsAsync(CancellationToken ct);
}
