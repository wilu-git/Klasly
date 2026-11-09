namespace Klasly.API.Services.Interfaces;

/// <summary>Creates and checks the rotating QR token <c>{sessionId}.{timeStep}.{signature}</c> (guide §27.8).</summary>
public interface IQrTokenService
{
    /// <summary>Length of one time step, from the <c>QrStepSeconds</c> setting.</summary>
    Task<int> StepSecondsAsync(CancellationToken ct);

    /// <summary>floor(Unix seconds now / stepSeconds), from the server clock.</summary>
    long CurrentStep(int stepSeconds);

    string Create(int sessionId, long step, string nonce);

    /// <summary>Reads the session id and step; false when the token isn't three well-formed parts.</summary>
    bool TryParse(string? token, out int sessionId, out long step);

    /// <summary>True when the token's signature matches the session's nonce and the signing key.</summary>
    bool HasValidSignature(string token, int sessionId, long step, string nonce);
}
