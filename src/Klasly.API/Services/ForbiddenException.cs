namespace Klasly.API.Services;

/// <summary>Thrown by a service when the caller is logged in but doesn't own the resource. Becomes a 403.</summary>
public class ForbiddenException(string message) : Exception(message);
