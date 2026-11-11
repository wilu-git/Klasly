namespace Klasly.Web.Models.Api;

/// <summary>Body of <c>POST /api/auth/login</c> (copy of the API's shape; Klasly.Web may not reference Klasly.Core).</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Returned by a successful login. <see cref="Role"/> is Admin, Teacher, Student or Parent.</summary>
public record LoginResponse(string Token, DateTime ExpiresAtUtc, int UserId, string FullName, string Role);
