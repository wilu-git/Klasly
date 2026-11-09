using System.ComponentModel.DataAnnotations;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>Class rules: subject, term and an active teacher must exist; one class per subject + section + term.</summary>
public class ClassService(
    IClassRepository classes, ISubjectRepository subjects, ITermRepository terms, IUserRepository users) : IClassService
{
    public async Task<List<ClassResponse>> ListAsync(int? termId, CancellationToken ct) =>
        (await classes.ListAsync(termId, ct)).Select(ToResponse).ToList();

    public async Task<ClassResponse> GetAsync(int classId, CancellationToken ct) =>
        ToResponse(await classes.GetDetailsAsync(classId, ct)
            ?? throw new KeyNotFoundException($"Class {classId} was not found."));

    public async Task<ClassResponse> CreateAsync(ClassRequest request, CancellationToken ct)
    {
        await ValidateAsync(request, null, ct);
        var cls = new Class();
        Apply(cls, request);
        await classes.AddAsync(cls, ct);
        return await GetAsync(cls.ClassId, ct); // reload with names for the response
    }

    public async Task<ClassResponse> UpdateAsync(int classId, ClassRequest request, CancellationToken ct)
    {
        var cls = await classes.GetByIdAsync(classId, ct)
            ?? throw new KeyNotFoundException($"Class {classId} was not found.");
        await ValidateAsync(request, classId, ct);
        Apply(cls, request);
        await classes.SaveChangesAsync(ct);
        return await GetAsync(classId, ct);
    }

    private async Task ValidateAsync(ClassRequest r, int? excludeClassId, CancellationToken ct)
    {
        if (await subjects.GetByIdAsync(r.SubjectId, ct) is null)
            throw new ValidationException($"Subject {r.SubjectId} does not exist.");
        if (await terms.GetByIdAsync(r.TermId, ct) is null)
            throw new ValidationException($"Term {r.TermId} does not exist.");
        var teacher = await users.GetTeacherAsync(r.TeacherId, ct)
            ?? throw new ValidationException($"Teacher {r.TeacherId} does not exist.");
        if (!teacher.User.IsActive)
            throw new ValidationException($"Teacher {teacher.User.FullName}'s account is inactive.");
        if (await classes.ExistsAsync(r.SubjectId, r.Section, r.TermId, excludeClassId, ct))
            throw new ConflictException($"A class for this subject, section '{r.Section}' and term already exists.");
    }

    private static void Apply(Class cls, ClassRequest r)
    {
        cls.SubjectId = r.SubjectId;
        cls.TeacherId = r.TeacherId;
        cls.TermId = r.TermId;
        cls.Section = r.Section;
        cls.Schedule = r.Schedule;
        cls.Room = r.Room;
    }

    private static ClassResponse ToResponse(Class c) => new(
        c.ClassId, c.SubjectId, c.Subject.Code, c.Subject.Title,
        c.TeacherId, c.Teacher.User.FullName, c.TermId, c.Term.Name,
        c.Section, c.Schedule, c.Room,
        c.Enrollments.Count); // only Active enrollments are loaded (filtered include)
}
