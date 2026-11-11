using System.Globalization;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.Worker.Services;

/// <summary>
/// The Worker's alert step (spec assumptions 2–6): turns recent absences into Pending Email rows in the Notifications
/// outbox, one per opted-in guardian, with the text written now in the school's time zone. One save per run.
/// </summary>
public class AlertQueuer(INotificationRepository notifications, ISystemSettingRepository settings)
{
    private static readonly TimeSpan AbsenceLookBack = TimeSpan.FromHours(48); // spec Q3

    public async Task<(int Absence, int AtRisk)> QueueAsync(DateTime nowUtc, CancellationToken ct)
    {
        var zone = await ReadTimeZoneAsync(ct);

        var absences = await notifications.ListAbsenceAlertsAsync(nowUtc - AbsenceLookBack, ct);
        foreach (var a in absences)
            notifications.Add(NewEmail(a.DedupKey, a.GuardianUserId, NotificationType.Absence, nowUtc,
                $"Klasly: {a.StudentName} was absent from {a.SubjectCode}",
                $"""
                 Good day, {a.GuardianName}.

                 {a.StudentName} was marked absent from {a.SubjectCode} {a.SubjectTitle}, section {a.Section}, in the class on {Format(a.StartTime, a.EndTime, zone)}.

                 The teacher marked or confirmed this absence in Klasly. If you have questions, please contact the teacher.

                 Klasly
                 """));

        await notifications.SaveChangesAsync(ct);
        return (absences.Count, 0); // at-risk alerts: Task 4
    }

    private static Notification NewEmail(string dedupKey, int userId, NotificationType type, DateTime nowUtc, string subject, string body) => new()
    {
        UserId = userId,
        Channel = NotificationChannel.Email,
        Type = type,
        Subject = subject,
        Body = body,
        Status = NotificationStatus.Pending,
        DedupKey = dedupKey,
        CreatedAt = nowUtc
    };

    // e.g. "Tue, 10 Nov 2026, 9:00 AM–10:30 AM (Asia/Manila)"
    private static string Format(DateTime startUtc, DateTime endUtc, TimeZoneInfo zone)
    {
        var start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), zone);
        var end = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(endUtc, DateTimeKind.Utc), zone);
        var c = CultureInfo.InvariantCulture;
        return $"{start.ToString("ddd, d MMM yyyy, h:mm tt", c)}–{end.ToString("h:mm tt", c)} ({zone.Id})";
    }

    private async Task<TimeZoneInfo> ReadTimeZoneAsync(CancellationToken ct)
    {
        var id = await settings.GetValueAsync("TimeZoneId", ct);
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id ?? throw new TimeZoneNotFoundException());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException("Setting 'TimeZoneId' is missing or invalid: it must be a time zone id, e.g. Asia/Manila.", ex);
        }
    }
}
