using System.ComponentModel.DataAnnotations;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>QR check-in. The checks run in exactly the order and with the messages of guide §27.8.</summary>
public class AttendanceService(
    IUserRepository users,
    ISessionRepository sessions,
    IEnrollmentRepository enrollments,
    IAttendanceRepository attendance,
    IQrTokenService qr) : IAttendanceService
{
    public async Task<ScanResponse> ScanAsync(int userId, string? role, ScanRequest request, CancellationToken ct)
    {
        // 1. Logged in as an active student.
        var user = role == nameof(UserRole.Student) ? await users.GetByIdAsync(userId, ct) : null;
        if (user is not { IsActive: true, Student: { } student })
            throw new UnauthorizedAccessException("Session expired – please log in");

        // 2. Well-formed token for an existing session, with a valid signature. An unknown session is
        //    "Invalid QR code", never a 404, so session ids can't be probed.
        if (!qr.TryParse(request.QrToken, out var sessionId, out var step))
            throw new ValidationException("Invalid QR code");
        var session = await sessions.GetByIdAsync(sessionId, ct);
        if (session is null || !qr.HasValidSignature(request.QrToken!, sessionId, step, session.QrNonce))
            throw new ValidationException("Invalid QR code");

        // 3. Current step or the one before (grace step).
        var current = qr.CurrentStep(await qr.StepSecondsAsync(ct));
        if (step != current && step != current - 1)
            throw new ValidationException("QR expired – rescan");

        // 4. Open and not past its end time.
        if (!SessionService.IsOpenNow(session))
            throw new ConflictException("Session is closed");

        // 5. Actively enrolled (a Dropped student is not).
        var enrollment = await enrollments.GetByClassAndStudentAsync(session.ClassId, student.StudentId, ct);
        if (enrollment?.Status != EnrollmentStatus.Active)
            throw new ForbiddenException("Not enrolled in this class");

        // 6. The phone registered after login. The scan never registers one.
        if (user.RegisteredDeviceId is null || user.RegisteredDeviceId != request.DeviceId)
            throw new ForbiddenException("Unregistered device");

        // 7. Once per session. The unique index catches two scans racing past this check.
        if (await attendance.ExistsAsync(session.SessionId, student.StudentId, ct))
            throw new ConflictException("Already checked in");

        var now = DateTime.UtcNow; // the server's clock, never the phone's
        var record = new AttendanceRecord
        {
            SessionId = session.SessionId,
            StudentId = student.StudentId,
            Status = now <= session.StartTime.AddMinutes(session.LateAfterMinutes)
                ? AttendanceStatus.Present
                : AttendanceStatus.Late,
            Source = AttendanceSource.Scan,
            ScannedAt = now,
            MarkedAt = now
        };
        if (!await attendance.TryAddAsync(record, ct))
            throw new ConflictException("Already checked in");

        return new ScanResponse(record.Status, now, session.Class.Subject.Code, session.Class.Section);
    }
}
