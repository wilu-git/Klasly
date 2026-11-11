using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>Admin management of enrollments. DELETE is a soft delete (Status = Dropped).</summary>
[ApiController]
[Route("api/enrollments")]
[Authorize(Policy = "AdminOnly")]
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    /// <summary>Active and Dropped enrollments of one class.</summary>
    [HttpGet("/api/classes/{classId:int}/enrollments")]
    public Task<List<EnrollmentResponse>> ListByClass(int classId, CancellationToken ct) =>
        enrollmentService.ListByClassAsync(classId, ct);

    /// <summary>201 for a new enrollment; 200 when a dropped student is re-enrolled.</summary>
    [HttpPost]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(EnrollmentRequest request, CancellationToken ct)
    {
        var (enrollment, created) = await enrollmentService.EnrollAsync(request, ct);
        return created
            ? CreatedAtAction(nameof(ListByClass), new { classId = enrollment.ClassId }, enrollment)
            : Ok(enrollment);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Drop(int id, CancellationToken ct)
    {
        await enrollmentService.DropAsync(id, ct);
        return NoContent();
    }
}
