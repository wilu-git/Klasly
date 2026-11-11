using Klasly.Core.Entities;

namespace Klasly.Core.Interfaces;

/// <summary>Data access for the Notifications outbox (push and email). DedupKey is unique.</summary>
public interface INotificationRepository
{
    /// <summary>Tracks the row; it is written by the next save on the same context.</summary>
    void Add(Notification notification);

    /// <summary>Ids of Push rows still Pending (picked up when the API starts).</summary>
    Task<List<int>> ListPendingPushIdsAsync(CancellationToken ct);

    /// <summary>Tracked, with User loaded; null when the row isn't a Pending push any more.</summary>
    Task<Notification?> GetPendingPushAsync(int notificationId, CancellationToken ct);

    /// <summary>
    /// Tracked Email rows still Pending whose retry time has come (<c>NextAttemptAt</c> null or ≤ now), oldest first,
    /// with User and Guardian loaded for the address. Push rows are never returned.
    /// </summary>
    Task<List<Notification>> ListDueEmailsAsync(DateTime nowUtc, int take, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
