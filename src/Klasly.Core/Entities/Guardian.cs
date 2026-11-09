namespace Klasly.Core.Entities;

/// <summary>dbo.Guardians — 1-to-1 with <see cref="User"/>.</summary>
public class Guardian
{
    public int GuardianId { get; set; }
    public int UserId { get; set; }

    /// <summary>Brevo alert recipient.</summary>
    public string ContactEmail { get; set; } = null!;

    public string? ContactNumber { get; set; }

    public User User { get; set; } = null!;
    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
}
