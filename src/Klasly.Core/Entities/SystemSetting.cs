namespace Klasly.Core.Entities;

/// <summary>dbo.SystemSettings — key/value tuning knobs. No secrets here.</summary>
public class SystemSetting
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public int? UpdatedBy { get; set; }

    /// <summary>UTC.</summary>
    public DateTime? UpdatedAt { get; set; }

    public User? UpdatedByUser { get; set; }
}
