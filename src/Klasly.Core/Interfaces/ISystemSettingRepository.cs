namespace Klasly.Core.Interfaces;

/// <summary>Reads the tunable key/value rows in SystemSettings (QR step, thresholds). Never secrets.</summary>
public interface ISystemSettingRepository
{
    /// <summary>The setting's raw value, or null when the key doesn't exist.</summary>
    Task<string?> GetValueAsync(string key, CancellationToken ct);
}
