namespace Klasly.Core.Entities;

/// <summary>Parent/guardian profile, one-to-one with <see cref="User"/>; receives Brevo alerts.</summary>
public class Guardian
{
    public int GuardianId { get; set; }
    public int UserId { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }

    public User User { get; set; } = null!;
    public ICollection<StudentGuardian> StudentGuardians { get; set; } = [];
}
