using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IGradeRepository"/>.</summary>
public class GradeRepository(KlaslyDbContext context) : IGradeRepository
{
    public Task<List<Grade>> ListByAssessmentAsync(int assessmentId, CancellationToken ct) =>
        context.Grades.Where(g => g.AssessmentId == assessmentId).ToListAsync(ct);

    public void Add(Grade grade) => context.Grades.Add(grade);

    public async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            context.ChangeTracker.Clear(); // the transaction rolled back; drop the batch so a later save can't resend it
            return false;
        }
    }
}
