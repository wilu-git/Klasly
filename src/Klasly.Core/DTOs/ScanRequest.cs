namespace Klasly.Core.DTOs;

/// <summary>
/// Body of <c>POST /api/attendance/scan</c>. No validation attributes on purpose: a bad value must fail the
/// guide's checks in their order (401 role → 400 "Invalid QR code" → … → 403 "Unregistered device"),
/// not as an earlier model-validation 400.
/// </summary>
public class ScanRequest
{
    /// <summary>The text read from the QR code: <c>{sessionId}.{timeStep}.{signature}</c>.</summary>
    public string? QrToken { get; init; }

    /// <summary>The phone's Android ID; must match the device registered after login.</summary>
    public string? DeviceId { get; init; }
}
