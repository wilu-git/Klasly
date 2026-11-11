using System.Net.Http.Json;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;

namespace Klasly.Tests;

/// <summary>The admin's Failed emails list (guide §27.8): only Email rows with Status = Failed, with the address and last error.</summary>
public class FailedEmailsTests(ApiFactory api, DatabaseFixture db)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Lists_failed_emails_only_with_the_guardian_address_and_last_error()
    {
        var failed = await AddAsync(NotificationChannel.Email, NotificationStatus.Failed, "Brevo 400: invalid email");
        var pending = await AddAsync(NotificationChannel.Email, NotificationStatus.Pending, null);
        var failedPush = await AddAsync(NotificationChannel.Push, NotificationStatus.Failed, "FCM 404");
        var admin = await api.LoginAsync("admin@klasly.test");

        var list = (await admin.GetFromJsonAsync<List<FailedEmailResponse>>("api/notifications/failed", Ct))!;

        var row = Assert.Single(list, r => r.NotificationId == failed);
        Assert.Equal(("Marites Mendoza", "marites.mendoza@klasly.test", "Absence", 5, "Brevo 400: invalid email"),
            (row.RecipientName, row.RecipientEmail, row.Type, row.Attempts, row.LastError));
        Assert.DoesNotContain(list, r => r.NotificationId == pending || r.NotificationId == failedPush);
        Assert.Equal(list.OrderByDescending(r => r.NotificationId), list); // newest first
    }

    private async Task<int> AddAsync(NotificationChannel channel, NotificationStatus status, string? lastError)
    {
        await using var context = db.CreateContext();
        var n = new Notification
        {
            UserId = 10, Channel = channel, Type = NotificationType.Absence, Subject = "Absence alert", Body = "Test",
            Status = status, Attempts = 5, LastError = lastError, DedupKey = $"test:{Guid.NewGuid():N}"
        };
        context.Notifications.Add(n);
        await context.SaveChangesAsync(Ct);
        return n.NotificationId;
    }
}
