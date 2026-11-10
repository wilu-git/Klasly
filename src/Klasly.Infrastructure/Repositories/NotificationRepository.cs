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

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
