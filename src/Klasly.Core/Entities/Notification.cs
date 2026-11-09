namespace Klasly.Core.Entities;

/// <summary>Outbox row for one push or email alert; <see cref="DedupKey"/> stops the same alert twice.</summary>
public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationType Type { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int Attempts { get; set; }

    /// <summary>UTC; backoff plus jitter set by the Worker.</summary>
    public DateTime? NextAttemptAt { get; set; }

    public string? LastError { get; set; }

    /// <summary>For example absent:{SessionId}:{StudentId}:{GuardianId}.</summary>
    public string DedupKey { get; set; } = string.Empty;

    /// <summary>Brevo messageId once accepted.</summary>
    public string? ExternalMessageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
