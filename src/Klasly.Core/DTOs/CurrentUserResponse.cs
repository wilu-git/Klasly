namespace Klasly.Core.DTOs;

/// <summary>Returned by <c>GET /api/auth/me</c>: who the bearer token belongs to.</summary>
public record CurrentUserResponse(int UserId, string Email, string FullName, string Role);
