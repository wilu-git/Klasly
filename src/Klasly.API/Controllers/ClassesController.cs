using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>Admin management of classes. No delete: classes have no soft-delete column.</summary>
[ApiController]
[Route("api/classes")]
[Authorize(Policy = "AdminOnly")]
public class ClassesController(IClassService classService) : ControllerBase
{
    /// <summary>All classes, or only those of <paramref name="termId"/>.</summary>
    [HttpGet]
    public Task<List<ClassResponse>> List([FromQuery] int? termId, CancellationToken ct) =>
        classService.ListAsync(termId, ct);

    [HttpGet("{id:int}")]
    public Task<ClassResponse> Get(int id, CancellationToken ct) => classService.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<ClassResponse>> Create(ClassRequest request, CancellationToken ct)
    {
        var cls = await classService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = cls.ClassId }, cls);
    }

    [HttpPut("{id:int}")]
    public Task<ClassResponse> Update(int id, ClassRequest request, CancellationToken ct) =>
        classService.UpdateAsync(id, request, ct);
}
