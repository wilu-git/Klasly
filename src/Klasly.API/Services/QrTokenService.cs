using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Klasly.API.Services.Interfaces;
using Klasly.Core.Interfaces;

namespace Klasly.API.Services;

/// <summary>
/// HMAC-SHA256 QR tokens. The per-session nonce is mixed in, so tokens of one session say nothing about another.
/// Signatures are compared in constant time.
/// </summary>
public class QrTokenService(IConfiguration config, ISystemSettingRepository settings) : IQrTokenService
{
    // Program.cs refuses to start when the key is missing or too short.
    private readonly byte[] key = Encoding.UTF8.GetBytes(config["Qr:SigningKey"]!);

    public async Task<int> StepSecondsAsync(CancellationToken ct) =>
        int.Parse(await settings.GetValueAsync("QrStepSeconds", ct)
            ?? throw new InvalidOperationException("SystemSettings row 'QrStepSeconds' is missing."),
            CultureInfo.InvariantCulture);

    public long CurrentStep(int stepSeconds) => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / stepSeconds;

    public string Create(int sessionId, long step, string nonce) => $"{sessionId}.{step}.{Sign(sessionId, step, nonce)}";

    public bool TryParse(string? token, out int sessionId, out long step)
    {
        sessionId = 0;
        step = 0;
        var parts = token?.Split('.');
        return parts is { Length: 3 }
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out sessionId)
            && long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out step)
            && parts[2].Length > 0;
    }

    public bool HasValidSignature(string token, int sessionId, long step, string nonce) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(token[(token.LastIndexOf('.') + 1)..]),
            Encoding.ASCII.GetBytes(Sign(sessionId, step, nonce)));

    private string Sign(int sessionId, long step, string nonce) =>
        Base64Url.EncodeToString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{sessionId}|{step}|{nonce}")));
}
