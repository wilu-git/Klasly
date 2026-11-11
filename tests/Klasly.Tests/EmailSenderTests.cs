using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Repositories;
using Klasly.Worker.Email;
using Klasly.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klasly.Tests;

/// <summary>
/// The Worker's email step (guide §27.8, spec assumption 8) against the real schema with a fake Brevo.
/// Every run sends all due Email rows in the database, so the class shares the "Outbox" collection and each test
/// gives its rows a unique subject: the fake answers by subject and the asserts look only at the test's own rows.
/// </summary>
[Collection("Outbox")]
public class EmailSenderTests(DatabaseFixture db)
{
    private const int GuardianUserId = 10; // Marites Mendoza, guardian 1
    private const int StudentUserId = 9;   // a student: no guardian profile, so no email address

    private static readonly DateTime Now = DateTime.UnixEpoch.AddSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Accepted_marks_the_row_Accepted_with_the_messageId()
    {
        var tag = NewTag();
        var id = await NewEmailAsync(tag);
        var brevo = new FakeBrevo(_ => new EmailResult(EmailOutcome.Accepted, "<msg-1@brevo>", null));

        await RunAsync(brevo, Now);

        var n = await GetAsync(id);
        Assert.Equal((NotificationStatus.Accepted, 1, "<msg-1@brevo>"), (n.Status, n.Attempts, n.ExternalMessageId));
        var sent = Assert.Single(brevo.Sent, s => s.Subject == tag);
        Assert.Equal(("marites.mendoza@klasly.test", "body " + tag), (sent.To, sent.Text));
    }

    [Fact]
    public async Task Permanent_marks_the_row_Failed_with_the_error_cut_to_500_chars()
    {
        var tag = NewTag();
        var id = await NewEmailAsync(tag);

        await RunAsync(new FakeBrevo(_ => new EmailResult(EmailOutcome.Permanent, null, "400 " + new string('x', 600))), Now);

        var n = await GetAsync(id);
        Assert.Equal((NotificationStatus.Failed, 1), (n.Status, n.Attempts));
        Assert.Equal(500, n.LastError!.Length);
        Assert.StartsWith("400 x", n.LastError);
    }

    [Fact]
    public async Task Transient_retries_with_growing_backoff_then_fails_at_MaxEmailAttempts()
    {
        var tag = NewTag();
        var id = await NewEmailAsync(tag);
        var brevo = new FakeBrevo(_ => new EmailResult(EmailOutcome.Transient, null, "503 busy"));

        await RunAsync(brevo, Now);
        var n = await GetAsync(id);
        Assert.Equal((NotificationStatus.Pending, 1, "503 busy"), (n.Status, n.Attempts, n.LastError));
        Assert.InRange(n.NextAttemptAt!.Value, Now.AddMinutes(5), Now.AddMinutes(6));

        var second = Now.AddMinutes(7);
        await RunAsync(brevo, second);
        n = await GetAsync(id);
        Assert.Equal((NotificationStatus.Pending, 2), (n.Status, n.Attempts));
        Assert.InRange(n.NextAttemptAt!.Value, second.AddMinutes(10), second.AddMinutes(11));

        var last = await NewEmailAsync(NewTag(), attempts: 4); // the seeded MaxEmailAttempts is 5
        await RunAsync(new FakeBrevo(_ => new EmailResult(EmailOutcome.Transient, null, "timeout")), Now);
        n = await GetAsync(last);
        Assert.Equal((NotificationStatus.Failed, 5, "timeout"), (n.Status, n.Attempts, n.LastError));
    }

    [Fact]
    public async Task Unauthorized_stops_the_batch_leaves_rows_untouched_and_logs_an_error()
    {
        var first = await NewEmailAsync(NewTag());
        var second = await NewEmailAsync(NewTag());
        var brevo = new FakeBrevo(_ => new EmailResult(EmailOutcome.Unauthorized, null, "401 Key not found"));
        var log = new ListLogger();

        var result = await RunAsync(brevo, Now, log);

        Assert.Single(brevo.Sent); // whichever row came first; nothing after it
        foreach (var id in new[] { first, second })
        {
            var n = await GetAsync(id);
            Assert.Equal((NotificationStatus.Pending, 0, (DateTime?)null), (n.Status, n.Attempts, n.NextAttemptAt));
        }
        Assert.Equal((0, 0, 0), result);
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("401 Key not found"));
    }

    [Fact]
    public async Task Without_Brevo_settings_due_rows_fail_without_a_send()
    {
        var id = await NewEmailAsync(NewTag());
        var brevo = new FakeBrevo(_ => throw new InvalidOperationException("must not send")) { IsConfigured = false };

        await RunAsync(brevo, Now);

        var n = await GetAsync(id);
        Assert.Equal((NotificationStatus.Failed, 0, "Brevo is not configured"), (n.Status, n.Attempts, n.LastError));
        Assert.Empty(brevo.Sent);
    }

    [Fact]
    public async Task A_row_whose_user_has_no_guardian_email_fails_without_a_send()
    {
        var tag = NewTag();
        var id = await NewEmailAsync(tag, userId: StudentUserId);
        var brevo = new FakeBrevo(_ => new EmailResult(EmailOutcome.Accepted, "x", null));

        await RunAsync(brevo, Now);

        Assert.Equal((NotificationStatus.Failed, "No guardian email"), Status(await GetAsync(id)));
        Assert.DoesNotContain(brevo.Sent, s => s.Subject == tag);
    }

    [Fact]
    public async Task Future_retries_push_rows_and_finished_rows_are_never_sent()
    {
        var future = NewTag();
        var push = NewTag();
        var done = NewTag();
        var futureId = await NewEmailAsync(future, nextAttemptAt: Now.AddHours(1));
        var pushId = await NewEmailAsync(push, channel: NotificationChannel.Push);
        await NewEmailAsync(done, status: NotificationStatus.Accepted);
        await NewEmailAsync(done + "-failed", status: NotificationStatus.Failed);
        var brevo = new FakeBrevo(_ => new EmailResult(EmailOutcome.Accepted, "x", null));

        await RunAsync(brevo, Now);

        Assert.DoesNotContain(brevo.Sent, s => s.Subject.StartsWith(future) || s.Subject.StartsWith(push) || s.Subject.StartsWith(done));
        foreach (var id in new[] { futureId, pushId })
        {
            var n = await GetAsync(id);
            Assert.Equal((NotificationStatus.Pending, 0), (n.Status, n.Attempts));
        }
    }

    [Fact]
    public async Task At_most_50_emails_are_sent_per_run()
    {
        // Send what other tests left due (e.g. alerts AlertQueuerTests queued), so only this test's 51 rows remain.
        var drain = new FakeBrevo(_ => new EmailResult(EmailOutcome.Accepted, "x", null));
        while (await RunAsync(drain, Now) is var r && r.Sent + r.Failed > 0) { }

        var tag = NewTag();
        await using (var context = db.CreateContext())
        {
            for (var i = 0; i < 51; i++) context.Notifications.Add(NewRow($"{tag}-{i}"));
            await context.SaveChangesAsync(Ct);
        }
        var brevo = new FakeBrevo(_ => new EmailResult(EmailOutcome.Accepted, "x", null));

        var (sent, _, _) = await RunAsync(brevo, Now);

        Assert.Equal(50, brevo.Sent.Count);
        Assert.Equal(50, sent);
        Assert.Contains(brevo.Sent, s => s.Subject == $"{tag}-0"); // oldest first
        Assert.DoesNotContain(brevo.Sent, s => s.Subject == $"{tag}-50");
    }

    [Fact]
    public async Task Each_row_is_saved_right_after_its_send()
    {
        var tag = NewTag();
        var first = await NewEmailAsync(tag + "-1");
        await NewEmailAsync(tag + "-2");
        var calls = 0;
        var brevo = new FakeBrevo(s => s.StartsWith(tag) && ++calls == 2
            ? throw new HttpRequestException("crash on the 2nd row")
            : new EmailResult(EmailOutcome.Accepted, "m-" + s, null));

        await Assert.ThrowsAsync<HttpRequestException>(() => RunAsync(brevo, Now));

        Assert.Equal(NotificationStatus.Accepted, (await GetAsync(first)).Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("five")]
    [InlineData("0")]
    [InlineData("21")]
    public async Task Missing_or_bad_MaxEmailAttempts_fails_the_step_and_sends_nothing(string? value)
    {
        var tag = NewTag();
        var id = await NewEmailAsync(tag);
        var brevo = new FakeBrevo(_ => new EmailResult(EmailOutcome.Accepted, "x", null));
        await using var context = db.CreateContext();
        var sender = new EmailSender(new NotificationRepository(context), brevo, new StubSettings(value), new ListLogger());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendDueAsync(Now, Ct));

        Assert.Contains("MaxEmailAttempts", error.Message);
        Assert.Empty(brevo.Sent);
        Assert.Equal(NotificationStatus.Pending, (await GetAsync(id)).Status);
    }

    // ---------- helpers ----------

    private static string NewTag() => $"t-{Guid.NewGuid():N}";

    private async Task<(int Sent, int Retrying, int Failed)> RunAsync(IBrevoEmailClient brevo, DateTime now, ListLogger? log = null)
    {
        await using var context = db.CreateContext();
        return await new EmailSender(new NotificationRepository(context), brevo, new SystemSettingRepository(context), log ?? new ListLogger())
            .SendDueAsync(now, Ct);
    }

    private static Notification NewRow(string subject, int userId = GuardianUserId, int attempts = 0, DateTime? nextAttemptAt = null,
        NotificationChannel channel = NotificationChannel.Email, NotificationStatus status = NotificationStatus.Pending) => new()
    {
        UserId = userId,
        Channel = channel,
        Type = NotificationType.Absence,
        Subject = subject,
        Body = "body " + subject,
        Status = status,
        Attempts = attempts,
        NextAttemptAt = nextAttemptAt,
        DedupKey = $"test:{Guid.NewGuid():N}"
    };

    private async Task<int> NewEmailAsync(string subject, int userId = GuardianUserId, int attempts = 0, DateTime? nextAttemptAt = null,
        NotificationChannel channel = NotificationChannel.Email, NotificationStatus status = NotificationStatus.Pending)
    {
        await using var context = db.CreateContext();
        var n = NewRow(subject, userId, attempts, nextAttemptAt, channel, status);
        context.Notifications.Add(n);
        await context.SaveChangesAsync(Ct);
        return n.NotificationId;
    }

    private async Task<Notification> GetAsync(int id)
    {
        await using var context = db.CreateContext();
        return await context.Notifications.AsNoTracking().SingleAsync(n => n.NotificationId == id, Ct);
    }

    private static (NotificationStatus, string?) Status(Notification n) => (n.Status, n.LastError);

    /// <summary>Answers each send by its subject; records every call.</summary>
    private sealed class FakeBrevo(Func<string, EmailResult> answer) : IBrevoEmailClient
    {
        public bool IsConfigured { get; init; } = true;
        public List<(string To, string Subject, string Text)> Sent { get; } = [];

        public Task<EmailResult> SendAsync(string toEmail, string toName, string subject, string text, CancellationToken ct)
        {
            Sent.Add((toEmail, subject, text));
            return Task.FromResult(answer(subject));
        }
    }

    private sealed class StubSettings(string? maxAttempts) : ISystemSettingRepository
    {
        public Task<string?> GetValueAsync(string key, CancellationToken ct) =>
            Task.FromResult(key == "MaxEmailAttempts" ? maxAttempts : null);
    }

    private sealed class ListLogger : ILogger<EmailSender>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
