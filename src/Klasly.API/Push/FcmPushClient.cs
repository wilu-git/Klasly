using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Klasly.API.Push;

/// <summary>The outcome of one FCM send: the message name FCM returns, or why it failed.</summary>
public record PushResult(bool Ok, string? MessageName, string? Error)
{
    public static PushResult Sent(string messageName) => new(true, messageName, null);
    public static PushResult Failed(string error) => new(false, null, error);
}

/// <summary>Sends one notification to one device through Firebase Cloud Messaging.</summary>
public interface IFcmPushClient
{
    /// <summary>False while <c>Fcm:ServiceAccountPath</c> isn't set (no Firebase project yet).</summary>
    bool IsConfigured { get; }

    Task<PushResult> SendAsync(string token, string title, string body, CancellationToken ct);
}

/// <summary>Typed HttpClient for FCM HTTP v1 (guide §13). HTTP errors come back as a <see cref="PushResult"/>, not an exception.</summary>
public class FcmPushClient(HttpClient http, FcmCredentials credentials) : IFcmPushClient
{
    public bool IsConfigured => credentials.IsConfigured;

    public async Task<PushResult> SendAsync(string token, string title, string body, CancellationToken ct)
    {
        var account = await credentials.GetAccountAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://fcm.googleapis.com/v1/projects/{account.ProjectId}/messages:send")
        {
            Content = JsonContent.Create(new { message = new { token, notification = new { title, body } } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await credentials.GetAccessTokenAsync(http, ct));

        using var response = await http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
            return PushResult.Sent((await response.Content.ReadFromJsonAsync<SendResponse>(ct))?.Name ?? "(no name)");

        // FCM's error body names the reason, e.g. UNREGISTERED for a token of an uninstalled app.
        return PushResult.Failed($"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync(ct)}");
    }

    private record SendResponse([property: JsonPropertyName("name")] string Name);
}
