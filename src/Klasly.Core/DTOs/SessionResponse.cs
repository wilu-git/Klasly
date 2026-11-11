using Klasly.Core.Entities;

namespace Klasly.Core.DTOs;

/// <summary>A class session. Times are UTC. Never carries the QR nonce.</summary>
public record SessionResponse(
    int SessionId,
    int ClassId,
    string SubjectCode,
    string Section,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    int LateAfterMinutes,
    SessionStatus Status,
    DateTime? ClosedAtUtc);
