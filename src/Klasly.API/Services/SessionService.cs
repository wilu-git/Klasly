using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>Session rules: only the class's own teacher manages its sessions; the end is after the start and in the future.</summary>
public class SessionService(
    ISessionRepository sessions, IClassRepository classes, IUserRepository users) : ISessionService
{
    public async Task<List<SessionResponse>> ListAsync(int classId, int userId, CancellationToken ct)
    {
        await EnsureOwnClassAsync(classId, userId, ct);
        return (await sessions.ListByClassAsync(classId, ct)).Select(ToResponse).ToList();
    }

    public async Task<SessionResponse> CreateAsync(
        int classId, CreateSessionRequest request, int userId, CancellationToken ct)
    {
        await EnsureOwnClassAsync(classId, userId, ct);
        var start = request.StartTime!.Value.UtcDateTime;
        var end = request.EndTime!.Value.UtcDateTime;
        if (end <= start) throw new ValidationException("The end time must be after the start time.");
        if (end <= DateTime.UtcNow) throw new ValidationException("The end time is already in the past.");

        var session = new ClassSession
        {
            ClassId = classId,
            StartTime = start,
            EndTime = end,
            LateAfterMinutes = request.LateAfterMinutes,
            QrNonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), // 64 chars, the column maximum
            Status = SessionStatus.Scheduled
        };
        await sessions.AddAsync(session, ct);
        return await GetAsync(session.SessionId, userId, ct); // reload with the subject for the response
    }

    public async Task<SessionResponse> GetAsync(int sessionId, int userId, CancellationToken ct) =>
        ToResponse(await GetOwnSessionAsync(sessionId, userId, ct));

    private async Task<int> TeacherIdAsync(int userId, CancellationToken ct) =>
        (await users.GetByIdAsync(userId, ct))?.Teacher?.TeacherId
        ?? throw new ForbiddenException("Only teachers can manage sessions.");

    private async Task EnsureOwnClassAsync(int classId, int userId, CancellationToken ct)
    {
        var cls = await classes.GetByIdAsync(classId, ct)
            ?? throw new KeyNotFoundException($"Class {classId} was not found.");
        if (cls.TeacherId != await TeacherIdAsync(userId, ct))
            throw new ForbiddenException("This class belongs to another teacher.");
    }

    private async Task<ClassSession> GetOwnSessionAsync(int sessionId, int userId, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(sessionId, ct)
            ?? throw new KeyNotFoundException($"Session {sessionId} was not found.");
        if (session.Class.TeacherId != await TeacherIdAsync(userId, ct))
            throw new ForbiddenException("This class belongs to another teacher.");
        return session;
    }

    // SQL hands dates back as Unspecified; they are stored as UTC, so stamp them for a "Z" in the JSON.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static SessionResponse ToResponse(ClassSession s) => new(
        s.SessionId, s.ClassId, s.Class.Subject.Code, s.Class.Section,
        Utc(s.StartTime), Utc(s.EndTime), s.LateAfterMinutes, s.Status,
        s.ClosedAt is { } closed ? Utc(closed) : null);
}
