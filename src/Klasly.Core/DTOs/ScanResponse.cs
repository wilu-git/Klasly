using Klasly.Core.Entities;

namespace Klasly.Core.DTOs;

/// <summary>A successful check-in: Present or Late, with the server's time (UTC) and the class.</summary>
public record ScanResponse(AttendanceStatus Status, DateTime ScannedAtUtc, string SubjectCode, string Section);
