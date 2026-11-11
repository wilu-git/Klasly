using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

public class OverviewService(IUserRepository users, IOverviewRepository overviews) : IOverviewService
{
    public async Task<StudentOverviewResponse> GetForStudentAsync(int studentUserId, CancellationToken ct)
    {
        var student = (await users.GetByIdAsync(studentUserId, ct))?.Student
            ?? throw new ForbiddenException("Only student accounts have their own records.");
        var overview = await GetAsync(student.StudentId, ct);
        // The at-risk flag is an early warning for teachers and parents (guide §27.5), not shown to the student.
        return overview with { Classes = overview.Classes.Select(c => c with { IsAtRisk = null }).ToList() };
    }

    public async Task<StudentOverviewResponse> GetForParentAsync(int parentUserId, int studentId, CancellationToken ct)
    {
        var guardian = (await users.GetByIdAsync(parentUserId, ct))?.Guardian;
        // Unknown ids get the same 403 as another family's child, so ids can't be probed.
        if (guardian is null || await users.GetLinkAsync(studentId, guardian.GuardianId, ct) is null)
            throw new ForbiddenException("You can only view your own children");
        return await GetAsync(studentId, ct);
    }

    private async Task<StudentOverviewResponse> GetAsync(int studentId, CancellationToken ct) =>
        await overviews.GetAsync(studentId, ct) ?? throw new KeyNotFoundException($"Student {studentId} was not found.");
}
