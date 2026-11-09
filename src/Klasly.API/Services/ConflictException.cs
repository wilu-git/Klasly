namespace Klasly.API.Services;

/// <summary>Thrown by a service when a value must be unique and is already taken. Becomes a 409.</summary>
public class ConflictException(string message) : Exception(message);
