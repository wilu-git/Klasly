using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IAttendanceRepository"/>.</summary>
public class AttendanceRepository(KlaslyDbContext context) : IAttendanceRepository
{
    public Task<bool> ExistsAsync(int sessionId, int studentId, CancellationToken ct) =>
        context.AttendanceRecords.AnyAsync(a => a.SessionId == sessionId && a.StudentId == studentId, ct);

    public async Task<bool> TryAddAsync(AttendanceRecord record, CancellationToken ct)
    {
        context.AttendanceRecords.Add(record);
        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            context.Entry(record).State = EntityState.Detached; // the other request's row won
            return false;
        }
    }
}
