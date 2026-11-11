using Klasly.Core.Entities;

namespace Klasly.Tests;

/// <summary>Rows every test can make for itself, so tests never share (and clash on) the same class.</summary>
public static class TestData
{
    // Seeded users (KlaslySeed): Rosa owns class 1 (teacher 1), Mark owns class 2 (teacher 2). Students 1–6 are users 4–9.
    public const int RosaUserId = 2;
    public const int MarkUserId = 3;

    /// <summary>A new class of Rosa's (seeded subject and term) with the given seats.</summary>
    public static async Task<int> NewClassAsync(DatabaseFixture db, params (int StudentId, EnrollmentStatus Status)[] seats)
    {
        await using var context = db.CreateContext();
        var cls = new Class { SubjectId = 1, TeacherId = 1, TermId = 1, Section = $"T-{Guid.NewGuid():N}"[..30] };
        foreach (var (studentId, status) in seats)
            cls.Enrollments.Add(new Enrollment { StudentId = studentId, Status = status });
        context.Classes.Add(cls);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return cls.ClassId;
    }
}
