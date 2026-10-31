using Klasly.Core.Enums;

namespace Klasly.Core.Entities;

/// <summary>dbo.Notifications — transactional outbox drained by the worker.</summary>
public class Notification
{
    public int NotificationId { get; set; }

    /// <summary>Recipient.</summary>
    public int UserId { get; set; }

    public NotificationChannel Channel { get; set; }
    public NotificationType Type { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int Attempts { get; set; }

    /// <summary>UTC; backoff plus jitter.</summary>
    public DateTime? NextAttemptAt { get; set; }

    public string? LastError { get; set; }

    /// <summary>e.g. absent:{SessionId}:{StudentId}:{GuardianId} — unique, so an alert is never sent twice.</summary>
    public string DedupKey { get; set; } = null!;

    /// <summary>Brevo messageId.</summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
