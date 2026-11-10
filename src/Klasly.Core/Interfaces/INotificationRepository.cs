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

    Task SaveChangesAsync(CancellationToken ct);
}
