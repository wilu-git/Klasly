using System.ComponentModel.DataAnnotations;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>Term rules: unique name, end date after start date, only one active term.</summary>
public class TermService(ITermRepository terms) : ITermService
{
    public async Task<List<TermResponse>> ListAsync(CancellationToken ct) =>
        (await terms.ListAsync(ct)).Select(ToResponse).ToList();

    public async Task<TermResponse> GetAsync(int termId, CancellationToken ct) =>
        ToResponse(await FindAsync(termId, ct));

    public async Task<TermResponse> CreateAsync(TermRequest request, CancellationToken ct)
    {
        await ValidateAsync(request, null, ct);
        // New terms start inactive; the admin activates one explicitly.
        var term = new AcademicTerm
        {
            Name = request.Name,
            StartDate = request.StartDate!.Value,
            EndDate = request.EndDate!.Value
        };
        await terms.AddAsync(term, ct);
        return ToResponse(term);
    }

    public async Task<TermResponse> UpdateAsync(int termId, TermRequest request, CancellationToken ct)
    {
        var term = await FindAsync(termId, ct);
        await ValidateAsync(request, termId, ct);
        term.Name = request.Name;
        term.StartDate = request.StartDate!.Value;
        term.EndDate = request.EndDate!.Value;
        await terms.SaveChangesAsync(ct);
        return ToResponse(term);
    }

    public async Task<TermResponse> ActivateAsync(int termId, CancellationToken ct)
    {
        var term = await FindAsync(termId, ct);
        await terms.ActivateAsync(term, ct);
        return ToResponse(term);
    }

    public async Task DeactivateAsync(int termId, CancellationToken ct)
    {
        var term = await FindAsync(termId, ct);
        term.IsActive = false;
        await terms.SaveChangesAsync(ct);
    }

    private async Task<AcademicTerm> FindAsync(int termId, CancellationToken ct) =>
        await terms.GetByIdAsync(termId, ct)
        ?? throw new KeyNotFoundException($"Term {termId} was not found.");

    private async Task ValidateAsync(TermRequest request, int? excludeTermId, CancellationToken ct)
    {
        if (request.EndDate <= request.StartDate)
            throw new ValidationException("End date must be after start date.");
        if (await terms.NameExistsAsync(request.Name, excludeTermId, ct))
            throw new ConflictException($"Term name '{request.Name}' is already used.");
    }

    private static TermResponse ToResponse(AcademicTerm t) =>
        new(t.TermId, t.Name, t.StartDate, t.EndDate, t.IsActive);
}
