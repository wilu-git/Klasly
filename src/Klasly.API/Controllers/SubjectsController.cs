using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>Admin management of the subject catalog. No delete: subjects have no soft-delete column.</summary>
[ApiController]
[Route("api/subjects")]
[Authorize(Policy = "AdminOnly")]
public class SubjectsController(ISubjectService subjectService) : ControllerBase
{
    [HttpGet]
    public Task<List<SubjectResponse>> List(CancellationToken ct) => subjectService.ListAsync(ct);

    [HttpGet("{id:int}")]
    public Task<SubjectResponse> Get(int id, CancellationToken ct) => subjectService.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> Create(SubjectRequest request, CancellationToken ct)
    {
        var subject = await subjectService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = subject.SubjectId }, subject);
    }

    [HttpPut("{id:int}")]
    public Task<SubjectResponse> Update(int id, SubjectRequest request, CancellationToken ct) =>
        subjectService.UpdateAsync(id, request, ct);
}
