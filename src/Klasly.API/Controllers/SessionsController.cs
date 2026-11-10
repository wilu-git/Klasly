using System.Security.Claims;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>The teacher's side of attendance: my classes, their sessions, opening and the QR token.</summary>
[ApiController]
[Authorize(Policy = "TeacherOnly")]
public class SessionsController(ISessionService sessionService, IClassService classService) : ControllerBase
{
    /// <summary>The caller's own classes, active term first.</summary>
    [HttpGet("api/teacher/classes")]
    public Task<List<ClassResponse>> MyClasses(CancellationToken ct) =>
        classService.ListForTeacherAsync(CurrentUserId, ct);

    /// <summary>Sessions of one of the caller's classes, newest first.</summary>
    [HttpGet("api/classes/{classId:int}/sessions")]
    public Task<List<SessionResponse>> List(int classId, CancellationToken ct) =>
        sessionService.ListAsync(classId, CurrentUserId, ct);

    [HttpPost("api/classes/{classId:int}/sessions")]
    public async Task<ActionResult<SessionResponse>> Create(
        int classId, CreateSessionRequest request, CancellationToken ct)
    {
        var session = await sessionService.CreateAsync(classId, request, CurrentUserId, ct);
        return CreatedAtAction(nameof(Get), new { id = session.SessionId }, session);
    }

    [HttpGet("api/sessions/{id:int}")]
    public Task<SessionResponse> Get(int id, CancellationToken ct) => sessionService.GetAsync(id, CurrentUserId, ct);

    /// <summary>Starts check-in. Opening an already open session changes nothing.</summary>
    [HttpPost("api/sessions/{id:int}/open")]
    public Task<SessionResponse> Open(int id, CancellationToken ct) => sessionService.OpenAsync(id, CurrentUserId, ct);

    /// <summary>Ends check-in ("End session" button). Closing an already closed session changes nothing.</summary>
    [HttpPost("api/sessions/{id:int}/close")]
    public Task<SessionResponse> Close(int id, CancellationToken ct) => sessionService.CloseAsync(id, CurrentUserId, ct);

    /// <summary>The QR token to show now; call again at expiresAtUtc.</summary>
    [HttpGet("api/sessions/{id:int}/qr")]
    public Task<QrTokenResponse> Qr(int id, CancellationToken ct) => sessionService.GetQrAsync(id, CurrentUserId, ct);

    /// <summary>Live roster: every enrolled student, status null until they scan.</summary>
    [HttpGet("api/sessions/{id:int}/attendance")]
    public Task<List<SessionAttendanceRow>> Attendance(int id, CancellationToken ct) =>
        sessionService.GetAttendanceAsync(id, CurrentUserId, ct);

    /// <summary>Corrects one student's result after the session has ended; returns the updated roster row.</summary>
    [HttpPut("api/sessions/{id:int}/attendance/{studentId:int}")]
    public Task<SessionAttendanceRow> CorrectAttendance(
        int id, int studentId, CorrectAttendanceRequest request, CancellationToken ct) =>
        sessionService.CorrectAttendanceAsync(id, studentId, request.Status!.Value, CurrentUserId, ct);

    // The TeacherOnly policy guarantees a valid token here.
    private int CurrentUserId => int.Parse(User.FindFirstValue("sub")!);
}
