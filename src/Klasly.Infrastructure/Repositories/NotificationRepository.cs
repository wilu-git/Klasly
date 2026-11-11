using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="INotificationRepository"/>.</summary>
public class NotificationRepository(KlaslyDbContext context) : INotificationRepository
{
    public void Add(Notification notification) => context.Notifications.Add(notification);

    public Task<List<int>> ListPendingPushIdsAsync(CancellationToken ct) =>
        context.Notifications
            .Where(n => n.Channel == NotificationChannel.Push && n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.NotificationId)
            .Select(n => n.NotificationId)
            .ToListAsync(ct);

    public Task<Notification?> GetPendingPushAsync(int notificationId, CancellationToken ct) =>
        context.Notifications.Include(n => n.User).FirstOrDefaultAsync(n =>
            n.NotificationId == notificationId && n.Channel == NotificationChannel.Push && n.Status == NotificationStatus.Pending, ct);

    public Task<List<Notification>> ListDueEmailsAsync(DateTime nowUtc, int take, CancellationToken ct) =>
        context.Notifications
            .Include(n => n.User).ThenInclude(u => u.Guardian)
            .Where(n => n.Channel == NotificationChannel.Email && n.Status == NotificationStatus.Pending
                        && (n.NextAttemptAt == null || n.NextAttemptAt <= nowUtc))
            .OrderBy(n => n.NotificationId) // identity order = oldest first
            .Take(take)
            .ToListAsync(ct);

    public Task<List<AbsenceAlert>> ListAbsenceAlertsAsync(DateTime sinceUtc, CancellationToken ct) =>
        (from r in context.AttendanceRecords
         where r.Status == AttendanceStatus.Absent && r.MarkedAt >= sinceUtc
               && r.Session.Status == SessionStatus.Finalized && r.Session.Class.Term.IsActive
               && r.Session.Class.Enrollments.Any(e => e.StudentId == r.StudentId && e.Status == EnrollmentStatus.Active)
         from sg in r.Student.StudentGuardians
         where sg.ReceivesAlerts && sg.Guardian.User.IsActive
         let key = "absent:" + r.SessionId + ":" + r.StudentId + ":" + sg.GuardianId
         where !context.Notifications.Any(n => n.DedupKey == key) // never queue the same alert twice
         select new AbsenceAlert(key, sg.Guardian.UserId, sg.Guardian.User.FullName, r.Student.User.FullName,
             r.Session.Class.Subject.Code, r.Session.Class.Subject.Title, r.Session.Class.Section,
             r.Session.StartTime, r.Session.EndTime))
        .ToListAsync(ct);

    public Task<List<AtRiskAlert>> ListAtRiskAlertsAsync(CancellationToken ct) =>
        (from e in context.Enrollments
         where e.Status == EnrollmentStatus.Active && e.Class.Term.IsActive
         let latest = e.RiskFlags.OrderByDescending(f => f.ComputedAt).ThenByDescending(f => f.RiskFlagId).FirstOrDefault()
         let previousAtRisk = e.RiskFlags.OrderByDescending(f => f.ComputedAt).ThenByDescending(f => f.RiskFlagId)
             .Skip(1).Select(f => (bool?)f.IsAtRisk).FirstOrDefault()
         where latest != null && latest.IsAtRisk && previousAtRisk != true // newly at-risk: no repeat while it stays so
         from sg in e.Student.StudentGuardians
         where sg.ReceivesAlerts && sg.Guardian.User.IsActive
         let key = "atrisk:" + latest.RiskFlagId + ":" + sg.GuardianId
         where !context.Notifications.Any(n => n.DedupKey == key)
         select new AtRiskAlert(key, sg.Guardian.UserId, sg.Guardian.User.FullName, e.Student.User.FullName,
             e.Class.Subject.Code, e.Class.Subject.Title, e.Class.Section, latest.AbsenceRate, latest.WeightedAverage))
        .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
