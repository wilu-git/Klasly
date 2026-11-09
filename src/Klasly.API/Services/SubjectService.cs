using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>Subject catalog rules: subject code must be unique.</summary>
public class SubjectService(ISubjectRepository subjects) : ISubjectService
{
    public async Task<List<SubjectResponse>> ListAsync(CancellationToken ct) =>
        (await subjects.ListAsync(ct)).Select(ToResponse).ToList();

    public async Task<SubjectResponse> GetAsync(int subjectId, CancellationToken ct) =>
        ToResponse(await FindAsync(subjectId, ct));

    public async Task<SubjectResponse> CreateAsync(SubjectRequest request, CancellationToken ct)
    {
        await EnsureCodeFreeAsync(request.Code, null, ct);
        var subject = new Subject { Code = request.Code, Title = request.Title, Units = request.Units };
        await subjects.AddAsync(subject, ct);
        return ToResponse(subject);
    }

    public async Task<SubjectResponse> UpdateAsync(int subjectId, SubjectRequest request, CancellationToken ct)
    {
        var subject = await FindAsync(subjectId, ct);
        await EnsureCodeFreeAsync(request.Code, subjectId, ct);
        subject.Code = request.Code;
        subject.Title = request.Title;
        subject.Units = request.Units;
        await subjects.SaveChangesAsync(ct);
        return ToResponse(subject);
    }

    private async Task<Subject> FindAsync(int subjectId, CancellationToken ct) =>
        await subjects.GetByIdAsync(subjectId, ct)
        ?? throw new KeyNotFoundException($"Subject {subjectId} was not found.");

    private async Task EnsureCodeFreeAsync(string code, int? excludeSubjectId, CancellationToken ct)
    {
        if (await subjects.CodeExistsAsync(code, excludeSubjectId, ct))
            throw new ConflictException($"Subject code '{code}' is already used.");
    }

    private static SubjectResponse ToResponse(Subject s) => new(s.SubjectId, s.Code, s.Title, s.Units);
}
