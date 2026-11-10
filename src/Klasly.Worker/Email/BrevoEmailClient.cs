using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Klasly.Worker.Email;

/// <summary>The <c>Brevo</c> config section. <see cref="ApiKey"/> is a user-secret (guide §14); the rest is plain config.</summary>
public class BrevoOptions
{
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3/";
    public string? ApiKey { get; set; }

    /// <summary>Must be a sender verified in the Brevo account.</summary>
    public string? SenderEmail { get; set; }

    public string SenderName { get; set; } = "Klasly";
}

/// <summary>How the email sender must treat one Brevo answer (guide §27.8 results).</summary>
public enum EmailOutcome
{
    /// <summary>2xx: Brevo took the email.</summary>
    Accepted,
    /// <summary>400 and other 4xx: this email will never go through.</summary>
    Permanent,
    /// <summary>429, 5xx, timeout, network error: try again later.</summary>
    Transient,
    /// <summary>401/403: our API key or account is wrong, not this email (spec Q2).</summary>
    Unauthorized
}

/// <summary>The outcome of one send: Brevo's messageId when accepted, otherwise why not.</summary>
public record EmailResult(EmailOutcome Outcome, string? MessageId, string? Error);

/// <summary>Sends one plain-text email through Brevo's transactional API.</summary>
public interface IBrevoEmailClient
{
    /// <summary>False until <c>Brevo:ApiKey</c> and <c>Brevo:SenderEmail</c> are both set.</summary>
    bool IsConfigured { get; }

    Task<EmailResult> SendAsync(string toEmail, string toName, string subject, string text, CancellationToken ct);
}

/// <summary>
/// Typed HttpClient for <c>POST /v3/smtp/email</c> (guide §13). Base address and the 10 s timeout are set at registration.
/// HTTP errors, network errors and timeouts come back as an <see cref="EmailResult"/>; only the caller's cancellation throws.
/// </summary>
public class BrevoEmailClient(HttpClient http, IOptions<BrevoOptions> options) : IBrevoEmailClient
{
    private readonly BrevoOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey) && !string.IsNullOrWhiteSpace(_options.SenderEmail);

    public async Task<EmailResult> SendAsync(string toEmail, string toName, string subject, string text, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = _options.SenderName, email = _options.SenderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                textContent = text
            })
        };
        request.Headers.Add("api-key", _options.ApiKey);

        try
        {
            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
                return new EmailResult(EmailOutcome.Accepted, MessageIdFrom(body), null);

            var status = (int)response.StatusCode;
            var outcome = status switch
            {
                401 or 403 => EmailOutcome.Unauthorized,
                429 or >= 500 => EmailOutcome.Transient,
                _ => EmailOutcome.Permanent
            };
            return new EmailResult(outcome, null, $"{status} {body}");
        }
        catch (HttpRequestException ex)
        {
            return new EmailResult(EmailOutcome.Transient, null, $"network error: {ex.Message}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new EmailResult(EmailOutcome.Transient, null, "timeout"); // HttpClient.Timeout, not our own cancellation
        }
    }

    // Brevo already took the email, so an odd body must not turn it into a failure (that would send it twice).
    private static string MessageIdFrom(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.GetProperty("messageId").GetString() ?? "(no messageId)";
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return "(no messageId)";
        }
    }
}
