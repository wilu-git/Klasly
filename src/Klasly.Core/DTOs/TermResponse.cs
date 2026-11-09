namespace Klasly.Core.DTOs;

/// <summary>An academic term. Only one term has <see cref="IsActive"/> = true at a time.</summary>
public record TermResponse(int TermId, string Name, DateOnly StartDate, DateOnly EndDate, bool IsActive);
