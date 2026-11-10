using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Push;

/// <summary>
/// Sends one Pending push row and records the outcome: Accepted (with FCM's message name) or Failed (with the reason).
/// shortcut: one attempt, no retries, and a stale (UNREGISTERED) token is not cleared; add both if pushes go missing in practice.
/// </summary>
public class PushNotificationSender(INotificationRepository notifications, IFcmPushClient fcm)
{
    public async Task SendAsync(int notificationId, CancellationToken ct)
    {
        var n = await notifications.GetPendingPushAsync(notificationId, ct);
        if (n is null) return; // already sent or failed (queued twice), nothing to do

        n.Attempts++;
        if (!fcm.IsConfigured) Fail(n, "FCM is not configured");
        else if (string.IsNullOrEmpty(n.User.FcmToken)) Fail(n, "No device registered for push");
        else
        {
            try
            {
                var result = await fcm.SendAsync(n.User.FcmToken, n.Subject ?? "Klasly", n.Body ?? "", ct);
                if (result.Ok)
                {
                    n.Status = NotificationStatus.Accepted;
                    n.ExternalMessageId = result.MessageName;
                }
                else Fail(n, result.Error ?? "FCM refused the push.");
            }
            catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                Fail(n, e.Message); // network error, timeout, unreadable service-account file
            }
        }
        await notifications.SaveChangesAsync(ct);
    }

    private static void Fail(Notification n, string reason)
    {
        n.Status = NotificationStatus.Failed;
        n.LastError = reason.Length > 500 ? reason[..500] : reason; // the column's length
    }
}
