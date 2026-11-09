namespace Klasly.Core.DTOs;

/// <summary>A subject in the course catalog.</summary>
public record SubjectResponse(int SubjectId, string Code, string Title, int Units);
