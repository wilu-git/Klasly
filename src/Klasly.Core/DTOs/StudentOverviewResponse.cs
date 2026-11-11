namespace Klasly.Core.DTOs;

/// <summary>
/// One student's active-term classes, returned by <c>GET /api/students/me/overview</c> and
/// <c>GET /api/parent/children/{studentId}/overview</c>. <c>TermName</c> null = no active term (Classes empty).
/// </summary>
public record StudentOverviewResponse(
    int StudentId, string StudentNumber, string FullName, string? TermName, List<ClassOverview> Classes);

/// <summary>
/// One Active enrollment. Counts and lists are live; the four risk fields come from the latest RiskFlags
/// snapshot and are all null until the Worker has computed one. Students get <c>IsAtRisk</c> null (spec Q2).
/// </summary>
public record ClassOverview(
    int ClassId, string SubjectCode, string SubjectTitle, string Section, string TeacherName,
    int Present, int Late, int Absent, int Excused,
    decimal? AbsenceRate, decimal? WeightedAverage, bool? IsAtRisk, DateTime? RiskComputedAtUtc,
    List<AttendanceItem> Attendance, List<GradeItem> Grades);

/// <summary>One attendance record, newest session first. <c>Status</c> is Present, Late, Absent or Excused.</summary>
public record AttendanceItem(int SessionId, DateTime StartTimeUtc, string Status);

/// <summary>One assessment of the class. <c>Score</c> and <c>PostedAtUtc</c> null = not graded yet.</summary>
public record GradeItem(
    int AssessmentId, string Title, string Type, decimal MaxScore, decimal WeightPercent, decimal? Score, DateTime? PostedAtUtc);
