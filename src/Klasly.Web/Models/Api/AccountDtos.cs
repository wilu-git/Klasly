namespace Klasly.Web.Models.Api;

/// <summary>Body of <c>POST /api/accounts</c>. Role is Admin, Teacher, Student or Parent; the API checks the role's fields.</summary>
public record CreateAccountRequest(
    string FullName, string Email, string Password, string Role,
    string? StudentNumber, int? YearLevel, string? Program,
    string? EmployeeNo, string? Department,
    string? ContactEmail, string? ContactNumber);

/// <summary>Body of <c>PUT /api/accounts/{id}</c>. <see cref="NewPassword"/> null = unchanged.</summary>
public record UpdateAccountRequest(
    string FullName, string Email, bool IsActive, string? NewPassword,
    string? StudentNumber, int? YearLevel, string? Program,
    string? EmployeeNo, string? Department,
    string? ContactEmail, string? ContactNumber);

public record AccountResponse(
    int UserId, string FullName, string Email, string Role, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt,
    int? StudentId, string? StudentNumber, int? YearLevel, string? Program,
    int? TeacherId, string? EmployeeNo, string? Department,
    int? GuardianId, string? ContactEmail, string? ContactNumber);

public record LinkStudentRequest(int StudentId, string? Relationship, bool ReceivesAlerts);

public record LinkedStudentResponse(int StudentId, string StudentNumber, string StudentName, string? Relationship, bool ReceivesAlerts);
