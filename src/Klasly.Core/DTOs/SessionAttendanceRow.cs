using Klasly.Core.Entities;

namespace Klasly.Core.DTOs;

/// <summary>One actively enrolled student on the live roster. <c>Status</c> null = hasn't scanned yet.</summary>
public record SessionAttendanceRow(
    int StudentId,
    string StudentNumber,
    string FullName,
    AttendanceStatus? Status,
    DateTime? ScannedAtUtc,
    AttendanceSource? Source);
