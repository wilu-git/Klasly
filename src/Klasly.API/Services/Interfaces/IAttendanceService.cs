using Klasly.Core.DTOs;

namespace Klasly.API.Services.Interfaces;

public interface IAttendanceService
{
    /// <summary>Runs the guide's 7 scan checks in order and records Present or Late.</summary>
    /// <param name="role">The JWT's role claim; anything but Student is a 401.</param>
    Task<ScanResponse> ScanAsync(int userId, string? role, ScanRequest request, CancellationToken ct);
}
