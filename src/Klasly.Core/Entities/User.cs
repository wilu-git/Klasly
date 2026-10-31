using Klasly.Core.Enums;

namespace Klasly.Core.Entities;

/// <summary>dbo.Users — one login per person, whatever their role.</summary>
public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;

    /// <summary>ASP.NET Core Identity hasher output, never plaintext.</summary>
    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; }
    public string? FcmToken { get; set; }

    /// <summary>One registered phone per student account.</summary>
    public string? RegisteredDeviceId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? UpdatedAt { get; set; }

    public Student? Student { get; set; }
    public Teacher? Teacher { get; set; }
    public Guardian? Guardian { get; set; }
    public ICollection<AttendanceRecord> MarkedAttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<ExcuseRequest> ReviewedExcuseRequests { get; set; } = new List<ExcuseRequest>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<SystemSetting> UpdatedSettings { get; set; } = new List<SystemSetting>();
}
