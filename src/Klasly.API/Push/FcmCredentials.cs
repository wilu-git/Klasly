using System.Buffers.Text;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Klasly.API.Push;

/// <summary>Settings for the grade push. <c>Fcm:ServiceAccountPath</c> lives in user-secrets, never in the repo.</summary>
public class FcmOptions
{
    /// <summary>Path to the Firebase service-account JSON file (kept outside the repo).</summary>
    public string? ServiceAccountPath { get; set; }
}

/// <summary>The fields of the Firebase service-account JSON file that the push needs.</summary>
public record ServiceAccount(
    [property: JsonPropertyName("project_id")] string ProjectId,
    [property: JsonPropertyName("client_email")] string ClientEmail,
    [property: JsonPropertyName("private_key")] string PrivateKey,
    [property: JsonPropertyName("token_uri")] string TokenUri);

/// <summary>
/// Gets Google OAuth access tokens for FCM HTTP v1 from the service account: sign an RS256 JWT with the account's key and
/// exchange it at Google's token URL (OAuth 2.0 JWT bearer grant). Built on .NET's own RSA and System.Text.Json instead
/// of Google.Apis.Auth (spec answer 4). Singleton: the token is cached until 5 minutes before it expires.
/// </summary>
public sealed class FcmCredentials(IOptions<FcmOptions> options)
{
    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";

    private readonly SemaphoreSlim gate = new(1, 1);
    private ServiceAccount? account;
    private string? accessToken;
    private DateTimeOffset renewAt;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ServiceAccountPath);

    public async Task<ServiceAccount> GetAccountAsync(CancellationToken ct)
    {
        if (account is not null) return account;
        await using var file = File.OpenRead(options.Value.ServiceAccountPath!);
        return account = await JsonSerializer.DeserializeAsync<ServiceAccount>(file, cancellationToken: ct)
            ?? throw new InvalidOperationException("The Firebase service-account file is empty.");
    }

    public async Task<string> GetAccessTokenAsync(HttpClient http, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (accessToken is not null && DateTimeOffset.UtcNow < renewAt) return accessToken;

            var sa = await GetAccountAsync(ct);
            using var response = await http.PostAsync(sa.TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = CreateAssertion(sa, DateTimeOffset.UtcNow)
            }), ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Google token request failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync(ct)}");

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
                ?? throw new HttpRequestException("Google sent an empty token response.");
            accessToken = token.AccessToken;
            renewAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn - 300);
            return accessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>The signed JWT Google exchanges for an access token, valid for one hour from <paramref name="now"/>.</summary>
    public static string CreateAssertion(ServiceAccount sa, DateTimeOffset now)
    {
        var header = Encode(new { alg = "RS256", typ = "JWT" });
        var iat = now.ToUnixTimeSeconds();
        var claims = Encode(new { iss = sa.ClientEmail, scope = Scope, aud = sa.TokenUri, iat, exp = iat + 3600 });
        var signingInput = $"{header}.{claims}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(sa.PrivateKey);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{signingInput}.{Base64Url.EncodeToString(signature)}";
    }

    private static string Encode(object value) => Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(value));

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
