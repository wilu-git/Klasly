using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>Admin management of academic terms. DELETE is a soft delete (IsActive = false).</summary>
[ApiController]
[Route("api/terms")]
[Authorize(Policy = "AdminOnly")]
public class TermsController(ITermService termService) : ControllerBase
{
    [HttpGet]
    public Task<List<TermResponse>> List(CancellationToken ct) => termService.ListAsync(ct);

    [HttpGet("{id:int}")]
    public Task<TermResponse> Get(int id, CancellationToken ct) => termService.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<TermResponse>> Create(TermRequest request, CancellationToken ct)
    {
        var term = await termService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = term.TermId }, term);
    }

    [HttpPut("{id:int}")]
    public Task<TermResponse> Update(int id, TermRequest request, CancellationToken ct) =>
        termService.UpdateAsync(id, request, ct);

    /// <summary>Makes this the current term; every other term becomes inactive.</summary>
    [HttpPost("{id:int}/activate")]
    public Task<TermResponse> Activate(int id, CancellationToken ct) => termService.ActivateAsync(id, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await termService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
