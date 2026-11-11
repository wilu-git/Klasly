using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

/// <summary>Dashboard data for students and parents (Milestone 13). Ownership comes from the token, never the client.</summary>
public interface IOverviewService
{
    /// <summary>The logged-in student's own overview, with <c>IsAtRisk</c> left out (spec Q2).</summary>
    Task<StudentOverviewResponse> GetForStudentAsync(int studentUserId, CancellationToken ct);

    /// <summary>A linked child's overview; any other id is a 403 (spec Q4).</summary>
    Task<StudentOverviewResponse> GetForParentAsync(int parentUserId, int studentId, CancellationToken ct);
}
