namespace Klasly.Web.Models.Api;

/// <summary>
/// Body of <c>GET /api/parent/children/{studentId}/overview</c>: one student's active-term classes.
/// <see cref="TermName"/> null = no active term (no classes).
/// </summary>
public record StudentOverviewResponse(
    int StudentId, string StudentNumber, string FullName, string? TermName, List<ClassOverview> Classes);

/// <summary>
/// One class. Counts and lists are live; the four risk fields are the Worker's latest snapshot and are
/// null until it has run. Times are UTC.
/// </summary>
public record ClassOverview(
    int ClassId, string SubjectCode, string SubjectTitle, string Section, string TeacherName,
    int Present, int Late, int Absent, int Excused,
    decimal? AbsenceRate, decimal? WeightedAverage, bool? IsAtRisk, DateTime? RiskComputedAtUtc,
    List<AttendanceItem> Attendance, List<GradeItem> Grades);

/// <summary>One attendance record, newest first. Status: Present, Late, Absent or Excused.</summary>
public record AttendanceItem(int SessionId, DateTime StartTimeUtc, string Status);

/// <summary>One assessment. <see cref="Score"/> null = not graded yet.</summary>
public record GradeItem(
    int AssessmentId, string Title, string Type, decimal MaxScore, decimal WeightPercent, decimal? Score, DateTime? PostedAtUtc);
