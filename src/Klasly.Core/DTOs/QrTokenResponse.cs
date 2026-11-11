namespace Klasly.Core.DTOs;

/// <summary>
/// The QR token to show now. Ask for the next one at <paramref name="ExpiresAtUtc"/>;
/// the token is still accepted for one more step after that (grace step).
/// </summary>
public record QrTokenResponse(string Token, DateTime ExpiresAtUtc, int StepSeconds);
