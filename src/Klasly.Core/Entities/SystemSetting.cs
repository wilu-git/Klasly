namespace Klasly.Core.Entities;

/// <summary>A tunable key/value setting (thresholds, QR step, time zone). Never holds secrets.</summary>
public class SystemSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User? UpdatedByUser { get; set; }
}
