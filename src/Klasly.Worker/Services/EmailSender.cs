using System.Globalization;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Worker.Email;
using Microsoft.Extensions.Logging;

namespace Klasly.Worker.Services;

/// <summary>
/// The Worker's last step (guide §27.8, spec assumption 8): sends due Email rows through Brevo and records each result.
/// Transient failures retry with backoff + jitter until MaxEmailAttempts; a 401/403 stops the batch (spec Q2).
/// </summary>
public class EmailSender(
    INotificationRepository notifications, IBrevoEmailClient brevo, ISystemSettingRepository settings, ILogger<EmailSender> logger)
{
    // shortcut: fixed cap (spec Q6) keeps a tick under the 10-min lock and Brevo's free 300/day; move to config if the school grows.
    private const int MaxPerRun = 50;

    public async Task<(int Sent, int Retrying, int Failed)> SendDueAsync(DateTime nowUtc, CancellationToken ct)
    {
        var maxAttempts = await ReadMaxAttemptsAsync(ct);
        var due = await notifications.ListDueEmailsAsync(nowUtc, MaxPerRun, ct);
        int sent = 0, retrying = 0, failed = 0;

        if (!brevo.IsConfigured) // spec Q4: fail them now so nothing piles up and blasts out the day the key is added
        {
            foreach (var n in due) Fail(n, "Brevo is not configured");
            await notifications.SaveChangesAsync(ct);
            return (0, 0, due.Count);
        }

        foreach (var n in due)
        {
            var guardian = n.User.Guardian;
            if (guardian is null)
            {
                Fail(n, "No guardian email");
                failed++;
            }
            else
            {
                var result = await brevo.SendAsync(guardian.ContactEmail, n.User.FullName, n.Subject ?? "Klasly", n.Body ?? "", ct);
                if (result.Outcome == EmailOutcome.Unauthorized)
                {
                    // Our key or account is wrong, not this email: leave every row Pending, uncounted, until it's fixed.
                    logger.LogError("Brevo rejected the API key ({Error}). Emails wait until it is fixed.", result.Error);
                    break;
                }

                n.Attempts++;
                switch (result.Outcome)
                {
                    case EmailOutcome.Accepted:
                        n.Status = NotificationStatus.Accepted;
                        n.ExternalMessageId = result.MessageId;
                        sent++;
                        break;
                    case EmailOutcome.Transient when n.Attempts < maxAttempts:
                        n.LastError = Cut(result.Error);
                        n.NextAttemptAt = nowUtc.AddMinutes(5 << (n.Attempts - 1)).AddSeconds(Random.Shared.Next(0, 61)); // 5, 10, 20… min + jitter
                        retrying++;
                        break;
                    default: // Permanent, or Transient with no attempts left
                        Fail(n, result.Error ?? result.Outcome.ToString());
                        failed++;
                        break;
                }
            }
            await notifications.SaveChangesAsync(ct); // per row: a crash later in the batch must not resend this one
        }

        return (sent, retrying, failed);
    }

    private static void Fail(Notification n, string reason)
    {
        n.Status = NotificationStatus.Failed;
        n.LastError = Cut(reason);
    }

    private static string? Cut(string? text) => text is { Length: > 500 } ? text[..500] : text; // the column's length

    private async Task<int> ReadMaxAttemptsAsync(CancellationToken ct) =>
        int.TryParse(await settings.GetValueAsync("MaxEmailAttempts", ct), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
        && value is >= 1 and <= 20
            ? value
            : throw new InvalidOperationException("Setting 'MaxEmailAttempts' is missing or invalid: it must be a whole number from 1 to 20, e.g. 5.");
}
