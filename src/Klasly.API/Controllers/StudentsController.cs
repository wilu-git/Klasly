using System.Security.Claims;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>Endpoints a student calls for their own account from the Android app.</summary>
[ApiController]
[Route("api/students/me")]
[Authorize(Policy = "StudentOnly")]
public class StudentsController(IAccountService accountService) : ControllerBase
{
    /// <summary>
    /// Registers this phone for scanning. The app calls it after every login: the first phone is saved,
    /// the same phone again changes nothing, another phone gets 409 until the admin resets it.
    /// </summary>
    [HttpPost("device")]
    public Task<DeviceResponse> RegisterDevice(RegisterDeviceRequest request, CancellationToken ct) =>
        accountService.RegisterDeviceAsync(CurrentUserId, request.DeviceId, ct);

    /// <summary>Saves this phone's FCM token for grade pushes. The app sends it after login and when Firebase renews it.</summary>
    [HttpPut("fcm-token")]
    public async Task<IActionResult> SetFcmToken(FcmTokenRequest request, CancellationToken ct)
    {
        await accountService.SetFcmTokenAsync(CurrentUserId, request.Token, ct);
        return NoContent();
    }

    // The StudentOnly policy guarantees a valid token here.
    private int CurrentUserId => int.Parse(User.FindFirstValue("sub")!);
}
