using Klasly.API.Push;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>
/// Assessment rules: own classes only; the class weight total never goes above 100% (grades may be posted before it
/// reaches 100, spec answer 1); a max score never drops below a posted score; at most 2 decimals (the columns' scale).
/// </summary>
public class AssessmentService(
    IAssessmentRepository assessments,
    IClassRepository classes,
    IUserRepository users,
    IEnrollmentRepository enrollments,
    IGradeRepository grades,
    INotificationRepository notifications,
    PushQueue pushQueue) : IAssessmentService
{
    public async Task<List<GradeRow>> GetGradesAsync(int assessmentId, int userId, CancellationToken ct)
    {
        var assessment = await GetOwnAssessmentAsync(assessmentId, userId, ct);
        return await GradeSheetAsync(assessment, ct);
    }

    /// <summary>
    /// Validates every row first and saves nothing if one is wrong. Then, in one save: new grades, changed scores,
    /// DatePosted on the first grade, and one Pending push row per student graded for the first time (spec answer 2).
    /// </summary>
    public async Task<SaveGradesResponse> SaveGradesAsync(int assessmentId, SaveGradesRequest request, int userId, CancellationToken ct)
    {
        var assessment = await GetOwnAssessmentAsync(assessmentId, userId, ct);
        var roster = (await enrollments.ListByClassAsync(assessment.ClassId, ct))
            .Where(e => e.Status == EnrollmentStatus.Active)
            .ToDictionary(e => e.StudentId);

        var errors = new Dictionary<string, string[]>();
        var seen = new HashSet<int>();
        for (var i = 0; i < request.Grades.Count; i++)
        {
            var row = request.Grades[i];
            if (!roster.ContainsKey(row.StudentId))
                errors[$"grades[{i}].studentId"] = ["This student is not enrolled in this class."];
            else if (!seen.Add(row.StudentId))
                errors[$"grades[{i}].studentId"] = ["This student is listed twice."];

            if (row.Score is not { } score) continue; // blank = not graded yet
            if (score < 0 || score > assessment.MaxScore)
                errors[$"grades[{i}].score"] = [$"Score must be between 0 and {Format(assessment.MaxScore)}."];
            else if (HasMoreThanTwoDecimals(score))
                errors[$"grades[{i}].score"] = ["Score can have at most 2 decimals."];
        }
        if (errors.Count > 0) throw new FieldValidationException(errors);

        var existing = (await grades.ListByAssessmentAsync(assessmentId, ct)).ToDictionary(g => g.StudentId);
        var now = DateTime.UtcNow;
        int added = 0, changed = 0;
        var pushes = new List<Notification>();
        foreach (var row in request.Grades)
        {
            if (row.Score is not { } score) continue;
            if (existing.TryGetValue(row.StudentId, out var grade))
            {
                if (grade.Score == score) continue; // the same score changes nothing
                grade.Score = score;
                grade.UpdatedAt = now;
                changed++;
                continue;
            }

            grades.Add(new Grade { AssessmentId = assessmentId, StudentId = row.StudentId, Score = score, PostedAt = now });
            var push = new Notification
            {
                UserId = roster[row.StudentId].Student.UserId,
                Channel = NotificationChannel.Push,
                Type = NotificationType.GradePosted,
                Status = NotificationStatus.Pending,
                Subject = "New grade posted",
                // Subject code, title and score only: no name or student number goes through Google (spec assumption 8).
                Body = $"{assessment.Class.Subject.Code} – {assessment.Title}: {Format(score)} / {Format(assessment.MaxScore)}",
                DedupKey = $"grade:{assessmentId}:{row.StudentId}",
                CreatedAt = now
            };
            notifications.Add(push);
            pushes.Add(push);
            added++;
        }

        if (added > 0) assessment.DatePosted ??= now;
        if (added + changed > 0 && !await grades.TrySaveAsync(ct))
            throw new ConflictException("Grades were just updated. Reload and try again.");
        foreach (var push in pushes) pushQueue.Enqueue(push.NotificationId); // sent in the background, after the commit

        return new SaveGradesResponse(added, changed, await GradeSheetAsync(assessment, ct));
    }

    private async Task<List<GradeRow>> GradeSheetAsync(Assessment assessment, CancellationToken ct)
    {
        var byStudent = (await grades.ListByAssessmentAsync(assessment.AssessmentId, ct)).ToDictionary(g => g.StudentId);
        return (await enrollments.ListByClassAsync(assessment.ClassId, ct)) // already sorted by name
            .Where(e => e.Status == EnrollmentStatus.Active)
            .Select(e =>
            {
                var g = byStudent.GetValueOrDefault(e.StudentId);
                return new GradeRow(e.StudentId, e.Student.StudentNumber, e.Student.User.FullName, g?.Score,
                    g is null ? null : Utc(g.PostedAt), g?.UpdatedAt is { } updated ? Utc(updated) : null);
            })
            .ToList();
    }

    private static string Format(decimal value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public async Task<List<AssessmentResponse>> ListAsync(int classId, int userId, CancellationToken ct)
    {
        await EnsureOwnClassAsync(classId, userId, ct);
        var graded = await assessments.CountGradedAsync(classId, ct);
        var active = await ActiveCountAsync(classId, ct);
        return (await assessments.ListByClassAsync(classId, ct))
            .Select(a => ToResponse(a, graded.GetValueOrDefault(a.AssessmentId), active))
            .ToList();
    }

    public async Task<AssessmentResponse> GetAsync(int assessmentId, int userId, CancellationToken ct) =>
        await ToResponseAsync(await GetOwnAssessmentAsync(assessmentId, userId, ct), ct);

    public async Task<AssessmentResponse> CreateAsync(int classId, AssessmentRequest request, int userId, CancellationToken ct)
    {
        await EnsureOwnClassAsync(classId, userId, ct);
        var title = request.Title.Trim();
        await ValidateAsync(classId, title, request, excludeAssessmentId: null, ct);

        var assessment = new Assessment
        {
            ClassId = classId,
            Title = title,
            Type = request.Type.Trim(),
            MaxScore = request.MaxScore!.Value,
            WeightPercent = request.WeightPercent!.Value
        };
        await assessments.AddAsync(assessment, ct);
        return await ToResponseAsync(assessment, ct);
    }

    public async Task<AssessmentResponse> UpdateAsync(int assessmentId, AssessmentRequest request, int userId, CancellationToken ct)
    {
        var assessment = await GetOwnAssessmentAsync(assessmentId, userId, ct);
        var title = request.Title.Trim();
        await ValidateAsync(assessment.ClassId, title, request, assessmentId, ct);

        var maxPosted = await assessments.MaxPostedScoreAsync(assessmentId, ct);
        if (maxPosted > request.MaxScore)
            throw new FieldValidationException("maxScore", $"Max score can't be lower than a posted score ({maxPosted:0.##}).");

        assessment.Title = title;
        assessment.Type = request.Type.Trim();
        assessment.MaxScore = request.MaxScore!.Value;
        assessment.WeightPercent = request.WeightPercent!.Value;
        await assessments.SaveChangesAsync(ct);
        return await ToResponseAsync(assessment, ct);
    }

    private async Task ValidateAsync(int classId, string title, AssessmentRequest request, int? excludeAssessmentId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (HasMoreThanTwoDecimals(request.MaxScore!.Value)) errors["maxScore"] = ["Max score can have at most 2 decimals."];
        if (HasMoreThanTwoDecimals(request.WeightPercent!.Value)) errors["weightPercent"] = ["Weight can have at most 2 decimals."];

        var others = await assessments.SumWeightsAsync(classId, excludeAssessmentId, ct);
        if (others + request.WeightPercent.Value > 100)
            errors["weightPercent"] = [$"The class weights can't go above 100%. {100 - others:0.##}% is left."];
        if (errors.Count > 0) throw new FieldValidationException(errors);

        if (await assessments.TitleExistsAsync(classId, title, excludeAssessmentId, ct))
            throw new ConflictException($"This class already has an assessment called \"{title}\".");
    }

    private static bool HasMoreThanTwoDecimals(decimal value) => decimal.Round(value, 2) != value;

    private async Task<int> ActiveCountAsync(int classId, CancellationToken ct) =>
        (await enrollments.ListByClassAsync(classId, ct)).Count(e => e.Status == EnrollmentStatus.Active);

    private async Task<AssessmentResponse> ToResponseAsync(Assessment a, CancellationToken ct) => ToResponse(
        a, (await assessments.CountGradedAsync(a.ClassId, ct)).GetValueOrDefault(a.AssessmentId), await ActiveCountAsync(a.ClassId, ct));

    // SQL hands dates back as Unspecified; they are stored as UTC, so stamp them for a "Z" in the JSON.
    private static AssessmentResponse ToResponse(Assessment a, int graded, int active) => new(
        a.AssessmentId, a.ClassId, a.Title, a.Type, a.MaxScore, a.WeightPercent,
        a.DatePosted is { } posted ? DateTime.SpecifyKind(posted, DateTimeKind.Utc) : null, graded, active);

    private async Task<int> TeacherIdAsync(int userId, CancellationToken ct) =>
        (await users.GetByIdAsync(userId, ct))?.Teacher?.TeacherId
        ?? throw new ForbiddenException("Only teachers can manage assessments.");

    private async Task EnsureOwnClassAsync(int classId, int userId, CancellationToken ct)
    {
        var cls = await classes.GetByIdAsync(classId, ct)
            ?? throw new KeyNotFoundException($"Class {classId} was not found.");
        if (cls.TeacherId != await TeacherIdAsync(userId, ct))
            throw new ForbiddenException("This class belongs to another teacher.");
    }

    private async Task<Assessment> GetOwnAssessmentAsync(int assessmentId, int userId, CancellationToken ct)
    {
        var assessment = await assessments.GetByIdAsync(assessmentId, ct)
            ?? throw new KeyNotFoundException($"Assessment {assessmentId} was not found.");
        if (assessment.Class.TeacherId != await TeacherIdAsync(userId, ct))
            throw new ForbiddenException("This class belongs to another teacher.");
        return assessment;
    }
}
