using Klasly.Web.Models.Api;

namespace Klasly.Web.Services.Interfaces;

public interface INotificationService
{
    /// <summary>Parent emails that failed for good, newest first.</summary>
    Task<List<FailedEmailResponse>> GetFailedEmailsAsync(CancellationToken ct);
}
