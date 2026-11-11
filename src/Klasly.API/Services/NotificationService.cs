using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

public class NotificationService(INotificationRepository notifications) : INotificationService
{
    private const int MaxRows = 100; // shortcut: no paging; add it if an admin ever needs more than the latest 100

    public Task<List<FailedEmailResponse>> ListFailedEmailsAsync(CancellationToken ct) =>
        notifications.ListFailedEmailsAsync(MaxRows, ct);
}
