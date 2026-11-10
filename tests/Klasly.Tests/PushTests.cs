using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Klasly.API.Push;
using Klasly.Core.Entities;
using Klasly.Infrastructure.Data;
using Klasly.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Tests;

/// <summary>
/// Sending a queued grade push (guide §27.8): Accepted with FCM's message name, or Failed with a reason. Never retried.
/// Uses seeded students that no other test class touches: Aira (user 4) gets a token here; Ellen (user 8) never has one.
/// </summary>
public class PushTests(DatabaseFixture db)
{
    private const int AiraUserId = 4;
    private const int EllenUserId = 8;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_sent_push_is_Accepted_with_the_FCM_message_name()
    {
        await SetTokenAsync(AiraUserId, "aira-test-token");
        var id = await NewPendingPushAsync(AiraUserId);
        var fcm = new FakeFcm { Result = PushResult.Sent("projects/klasly/messages/123") };

        await SendAsync(id, fcm);

        var n = await GetAsync(id);
        Assert.Equal((NotificationStatus.Accepted, "projects/klasly/messages/123", 1), (n.Status, n.ExternalMessageId, n.Attempts));
        Assert.Equal(("aira-test-token", "New grade posted", "ELECT3 – Quiz 1: 18 / 20"), fcm.Sent.Single());
    }

    [Fact]
    public async Task An_FCM_error_marks_the_push_Failed_with_the_reason()
    {
        await SetTokenAsync(AiraUserId, "aira-test-token");
        var id = await NewPendingPushAsync(AiraUserId);

        await SendAsync(id, new FakeFcm { Result = PushResult.Failed("404 UNREGISTERED") });

        var n = await GetAsync(id);
        Assert.Equal((NotificationStatus.Failed, "404 UNREGISTERED"), (n.Status, n.LastError));
    }

    [Fact]
    public async Task Without_Firebase_settings_the_push_fails_without_calling_FCM()
    {
        var id = await NewPendingPushAsync(AiraUserId);
        var fcm = new FakeFcm { IsConfigured = false };

        await SendAsync(id, fcm);

        Assert.Equal((NotificationStatus.Failed, "FCM is not configured"), Status(await GetAsync(id)));
        Assert.Empty(fcm.Sent);
    }

    [Fact]
    public async Task A_student_without_a_device_token_fails_without_calling_FCM()
    {
        var id = await NewPendingPushAsync(EllenUserId);
        var fcm = new FakeFcm();

        await SendAsync(id, fcm);

        Assert.Equal((NotificationStatus.Failed, "No device registered for push"), Status(await GetAsync(id)));
        Assert.Empty(fcm.Sent);
    }

    [Fact]
    public async Task A_push_that_is_no_longer_Pending_is_not_sent_again()
    {
        await SetTokenAsync(AiraUserId, "aira-test-token");
        var id = await NewPendingPushAsync(AiraUserId);
        await SendAsync(id, new FakeFcm());
        var fcm = new FakeFcm();

        await SendAsync(id, fcm); // e.g. the startup pickup and the request both queued it

        Assert.Empty(fcm.Sent);
        Assert.Equal(1, (await GetAsync(id)).Attempts);
    }

    [Fact]
    public async Task Pending_push_rows_are_found_for_the_startup_pickup()
    {
        var pending = await NewPendingPushAsync(EllenUserId);
        var done = await NewPendingPushAsync(EllenUserId);
        await SendAsync(done, new FakeFcm());

        await using var context = db.CreateContext();
        var ids = await new NotificationRepository(context).ListPendingPushIdsAsync(Ct);

        Assert.Contains(pending, ids);
        Assert.DoesNotContain(done, ids);
    }

    [Fact]
    public void The_Google_assertion_is_an_RS256_JWT_signed_with_the_service_account_key()
    {
        using var rsa = RSA.Create(2048);
        var account = new ServiceAccount("klasly-test", "push@klasly-test.iam.gserviceaccount.com", rsa.ExportPkcs8PrivateKeyPem(),
            "https://oauth2.googleapis.com/token");
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

        var parts = FcmCredentials.CreateAssertion(account, now).Split('.');

        Assert.Equal(3, parts.Length);
        using var header = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[0]));
        Assert.Equal("RS256", header.RootElement.GetProperty("alg").GetString());
        using var claims = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
        var c = claims.RootElement;
        Assert.Equal(account.ClientEmail, c.GetProperty("iss").GetString());
        Assert.Equal(account.TokenUri, c.GetProperty("aud").GetString());
        Assert.Equal("https://www.googleapis.com/auth/firebase.messaging", c.GetProperty("scope").GetString());
        Assert.Equal(1_800_000_000, c.GetProperty("iat").GetInt64());
        Assert.Equal(1_800_003_600, c.GetProperty("exp").GetInt64());
        Assert.True(rsa.VerifyData(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"), Base64Url.DecodeFromChars(parts[2]),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    // ---------- helpers ----------

    private sealed class FakeFcm : IFcmPushClient
    {
        public bool IsConfigured { get; init; } = true;
        public PushResult Result { get; init; } = PushResult.Sent("projects/klasly/messages/1");
        public List<(string Token, string Title, string Body)> Sent { get; } = [];

        public Task<PushResult> SendAsync(string token, string title, string body, CancellationToken ct)
        {
            Sent.Add((token, title, body));
            return Task.FromResult(Result);
        }
    }

    private async Task SendAsync(int notificationId, IFcmPushClient fcm)
    {
        await using var context = db.CreateContext();
        await new PushNotificationSender(new NotificationRepository(context), fcm).SendAsync(notificationId, Ct);
    }

    private async Task<int> NewPendingPushAsync(int userId)
    {
        await using var context = db.CreateContext();
        var n = new Notification
        {
            UserId = userId,
            Channel = NotificationChannel.Push,
            Type = NotificationType.GradePosted,
            Subject = "New grade posted",
            Body = "ELECT3 – Quiz 1: 18 / 20",
            DedupKey = $"test:{Guid.NewGuid():N}"
        };
        context.Notifications.Add(n);
        await context.SaveChangesAsync(Ct);
        return n.NotificationId;
    }

    private async Task SetTokenAsync(int userId, string token)
    {
        await using var context = db.CreateContext();
        await context.Users.Where(u => u.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.FcmToken, token), Ct);
    }

    private async Task<Notification> GetAsync(int id)
    {
        await using var context = db.CreateContext();
        return await context.Notifications.AsNoTracking().SingleAsync(n => n.NotificationId == id, Ct);
    }

    private static (NotificationStatus, string?) Status(Notification n) => (n.Status, n.LastError);
}
