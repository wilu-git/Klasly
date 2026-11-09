using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/students/me/device</c>: the phone's Android ID.</summary>
public class RegisterDeviceRequest
{
    [Required, MaxLength(200)]
    public string DeviceId { get; init; } = string.Empty;
}
