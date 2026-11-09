using System.Security.Claims;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>Admin management of accounts (soft delete) and parent ↔ student links.</summary>
[ApiController]
[Route("api/accounts")]
[Authorize(Policy = "AdminOnly")]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    /// <summary>All accounts, active and inactive; <paramref name="role"/> filters (Admin, Teacher, Student, Parent).</summary>
    [HttpGet]
    public Task<List<AccountResponse>> List([FromQuery] UserRole? role, CancellationToken ct) =>
        accountService.ListAsync(role, ct);

    [HttpGet("{id:int}")]
    public Task<AccountResponse> Get(int id, CancellationToken ct) => accountService.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var account = await accountService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = account.UserId }, account);
    }

    [HttpPut("{id:int}")]
    public Task<AccountResponse> Update(int id, UpdateAccountRequest request, CancellationToken ct) =>
        accountService.UpdateAsync(id, request, CurrentUserId, ct);

    /// <summary>Soft delete: the account stays but can no longer log in.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await accountService.DeactivateAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/students")]
    public Task<List<LinkedStudentResponse>> ListLinkedStudents(int id, CancellationToken ct) =>
        accountService.ListLinkedStudentsAsync(id, ct);

    [HttpPost("{id:int}/students")]
    public async Task<ActionResult<LinkedStudentResponse>> LinkStudent(
        int id, LinkStudentRequest request, CancellationToken ct)
    {
        var link = await accountService.LinkStudentAsync(id, request, ct);
        return CreatedAtAction(nameof(ListLinkedStudents), new { id }, link);
    }

    [HttpDelete("{id:int}/students/{studentId:int}")]
    public async Task<IActionResult> UnlinkStudent(int id, int studentId, CancellationToken ct)
    {
        await accountService.UnlinkStudentAsync(id, studentId, ct);
        return NoContent();
    }

    /// <summary>Clears the student's registered phone so their next login registers a new one.</summary>
    [HttpDelete("{id:int}/device")]
    public async Task<IActionResult> ResetDevice(int id, CancellationToken ct)
    {
        await accountService.ResetDeviceAsync(id, ct);
        return NoContent();
    }

    // Same claim /api/auth/me reads; the AdminOnly policy guarantees a valid token here.
    private int CurrentUserId => int.Parse(User.FindFirstValue("sub")!);
}
