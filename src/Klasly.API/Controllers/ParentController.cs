using System.Security.Claims;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>A parent's own children, for the Parent portal (MVC) and the Android Child overview.</summary>
[ApiController]
[Route("api/parent/children")]
[Authorize(Policy = "ParentOnly")]
public class ParentController(IAccountService accountService, IOverviewService overviewService) : ControllerBase
{
    /// <summary>The students linked to this parent, by name.</summary>
    [HttpGet]
    public Task<List<LinkedStudentResponse>> List(CancellationToken ct) =>
        accountService.ListLinkedStudentsAsync(CurrentUserId, ct);

    /// <summary>A linked child's active-term attendance, grades and at-risk status. Any other id → 403.</summary>
    [HttpGet("{studentId:int}/overview")]
    public Task<StudentOverviewResponse> Overview(int studentId, CancellationToken ct) =>
        overviewService.GetForParentAsync(CurrentUserId, studentId, ct);

    // The ParentOnly policy guarantees a valid token here.
    private int CurrentUserId => int.Parse(User.FindFirstValue("sub")!);
}
