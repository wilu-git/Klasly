namespace Klasly.Core.DTOs;

/// <summary>Returned by a successful login. <see cref="Role"/> is Admin, Teacher, Student or Parent.</summary>
public record LoginResponse(string Token, DateTime ExpiresAtUtc, int UserId, string FullName, string Role);
