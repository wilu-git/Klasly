using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>PUT /api/students/me/fcm-token</c>: this phone's Firebase Cloud Messaging token.</summary>
public class FcmTokenRequest
{
    [Required, MaxLength(500)]
    public string Token { get; init; } = string.Empty;
}
