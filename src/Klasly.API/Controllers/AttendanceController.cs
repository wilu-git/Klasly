using System.Security.Claims;
using Klasly.API.Services.Interfaces;
using Klasly.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klasly.API.Controllers;

/// <summary>The student's side of attendance: scanning the teacher's QR code.</summary>
[ApiController]
[Route("api/attendance")]
[Authorize] // not StudentOnly: the guide wants 401 (not 403) for another role, so the service checks the role
public class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    /// <summary>Checks the student in as Present or Late; every refusal carries a message the app shows.</summary>
    [HttpPost("scan")]
    public Task<ScanResponse> Scan(ScanRequest request, CancellationToken ct) =>
        attendanceService.ScanAsync(int.Parse(User.FindFirstValue("sub")!), User.FindFirstValue("role"), request, ct);
}
