namespace Klasly.Web.Models.Api;

// Mirror of the Web API's notification DTO (GET api/notifications/failed). Times are UTC.

public record FailedEmailResponse(
    int NotificationId, DateTime CreatedAtUtc, string RecipientName, string RecipientEmail,
    string Type, string? Subject, int Attempts, string? LastError);
