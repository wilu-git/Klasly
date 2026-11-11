using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ISessionRepository"/>.</summary>
public class SessionRepository(KlaslyDbContext context) : ISessionRepository
{
    public Task<ClassSession?> GetByIdAsync(int sessionId, CancellationToken ct) =>
        context.ClassSessions.Include(s => s.Class).ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

    public Task<List<ClassSession>> ListByClassAsync(int classId, CancellationToken ct) =>
        context.ClassSessions.AsNoTracking()
            .Include(s => s.Class).ThenInclude(c => c.Subject)
            .Where(s => s.ClassId == classId)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(ct);

    public Task<List<ClassSession>> ListEndedAsync(DateTime nowUtc, CancellationToken ct) =>
        context.ClassSessions
            .Where(s => (s.Status == SessionStatus.Open || s.Status == SessionStatus.Scheduled) && s.EndTime <= nowUtc)
            .ToListAsync(ct);

    public Task<List<ClassSession>> ListClosedAsync(CancellationToken ct) =>
        context.ClassSessions.Where(s => s.Status == SessionStatus.Closed).ToListAsync(ct);

    public async Task AddAsync(ClassSession session, CancellationToken ct)
    {
        context.ClassSessions.Add(session);
        await context.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}
