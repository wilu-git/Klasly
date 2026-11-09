namespace Klasly.Core.Entities;

/// <summary>A login account for any role. Soft-deleted by setting <see cref="IsActive"/> to false.</summary>
public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? FcmToken { get; set; }
    public string? RegisteredDeviceId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student? Student { get; set; }
    public Teacher? Teacher { get; set; }
    public Guardian? Guardian { get; set; }
    public ICollection<Notification> Notifications { get; set; } = [];
}
