namespace Klasly.Core.DTOs;

/// <summary>One absence email still to queue: one guardian, one Absent record. Times are UTC.</summary>
public record AbsenceAlert(
    string DedupKey,
    int GuardianUserId,
    string GuardianName,
    string StudentName,
    string SubjectCode,
    string SubjectTitle,
    string Section,
    DateTime StartTime,
    DateTime EndTime);
