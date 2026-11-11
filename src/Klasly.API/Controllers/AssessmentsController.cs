using System.Security.Claims;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>A teacher's assessments and their grades. The services check that the class is the caller's.</summary>
[ApiController]
[Authorize(Policy = "TeacherOnly")]
public class AssessmentsController(IAssessmentService assessmentService) : ControllerBase
{
    /// <summary>Assessments of one of the caller's classes, oldest first, with how many students are graded.</summary>
    [HttpGet("api/classes/{classId:int}/assessments")]
    public Task<List<AssessmentResponse>> List(int classId, CancellationToken ct) =>
        assessmentService.ListAsync(classId, CurrentUserId, ct);

    /// <summary>Adds an assessment. The class's weights can't go above 100% in total.</summary>
    [HttpPost("api/classes/{classId:int}/assessments")]
    public async Task<ActionResult<AssessmentResponse>> Create(int classId, AssessmentRequest request, CancellationToken ct)
    {
        var assessment = await assessmentService.CreateAsync(classId, request, CurrentUserId, ct);
        return CreatedAtAction(nameof(Get), new { id = assessment.AssessmentId }, assessment);
    }

    [HttpGet("api/assessments/{id:int}")]
    public Task<AssessmentResponse> Get(int id, CancellationToken ct) => assessmentService.GetAsync(id, CurrentUserId, ct);

    /// <summary>Edits an assessment. The max score can't drop below a score already posted.</summary>
    [HttpPut("api/assessments/{id:int}")]
    public Task<AssessmentResponse> Update(int id, AssessmentRequest request, CancellationToken ct) =>
        assessmentService.UpdateAsync(id, request, CurrentUserId, ct);

    /// <summary>The grade sheet: every actively enrolled student, score null until graded.</summary>
    [HttpGet("api/assessments/{id:int}/grades")]
    public Task<List<GradeRow>> Grades(int id, CancellationToken ct) => assessmentService.GetGradesAsync(id, CurrentUserId, ct);

    /// <summary>Saves the sheet. A student's first grade queues a push to their phone.</summary>
    [HttpPut("api/assessments/{id:int}/grades")]
    public Task<SaveGradesResponse> SaveGrades(int id, SaveGradesRequest request, CancellationToken ct) =>
        assessmentService.SaveGradesAsync(id, request, CurrentUserId, ct);

    // The TeacherOnly policy guarantees a valid token here.
    private int CurrentUserId => int.Parse(User.FindFirstValue("sub")!);
}
