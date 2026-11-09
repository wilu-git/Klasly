namespace Klasly.Core.DTOs;

/// <summary>A class with the subject, teacher and term names included, so a list page needs one call.</summary>
public record ClassResponse(
    int ClassId,
    int SubjectId,
    string SubjectCode,
    string SubjectTitle,
    int TeacherId,
    string TeacherName,
    int TermId,
    string TermName,
    string Section,
    string? Schedule,
    string? Room,
    int ActiveEnrollmentCount);
