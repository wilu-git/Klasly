namespace Klasly.Core.DTOs;

/// <summary>A student linked to a parent account.</summary>
public record LinkedStudentResponse(
    int StudentId, string StudentNumber, string StudentName, string? Relationship, bool ReceivesAlerts);
