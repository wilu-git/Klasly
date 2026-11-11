namespace Klasly.Web.Models.Api;

public record ClassRequest(int SubjectId, int TeacherId, int TermId, string Section, string? Schedule, string? Room);

public record ClassResponse(
    int ClassId, int SubjectId, string SubjectCode, string SubjectTitle,
    int TeacherId, string TeacherName, int TermId, string TermName,
    string Section, string? Schedule, string? Room, int ActiveEnrollmentCount);

public record EnrollmentRequest(int ClassId, int StudentId);

/// <summary><see cref="Status"/> is Active or Dropped.</summary>
public record EnrollmentResponse(int EnrollmentId, int ClassId, int StudentId, string StudentNumber, string StudentName, string Status);
