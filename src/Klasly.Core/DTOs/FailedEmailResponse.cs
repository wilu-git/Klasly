namespace Klasly.Core.DTOs;

/// <summary>One parent email that failed for good (guide §27.8), for the admin's Failed emails list. Times are UTC.</summary>
public record FailedEmailResponse(
    int NotificationId,
    DateTime CreatedAtUtc,
    string RecipientName,
    string RecipientEmail,
    string Type,
    string? Subject,
    int Attempts,
    string? LastError);
