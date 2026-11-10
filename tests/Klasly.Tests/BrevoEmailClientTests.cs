using System.Net;
using System.Text.Json;
using Klasly.Worker.Email;
using Microsoft.Extensions.Options;

namespace Klasly.Tests;

/// <summary>The Brevo client against a fake HTTP handler: no network, no real key (guide §13, §27.8 results).</summary>
public class BrevoEmailClientTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sends_one_post_with_the_api_key_and_the_brevo_json()
    {
        var handler = new StubHandler(HttpStatusCode.Created, """{ "messageId": "<abc@smtp-relay.brevo.com>" }""");

        var result = await Client(handler).SendAsync("parent@example.com", "Marites Mendoza", "Klasly: alert", "Hello", Ct);

        Assert.Equal(new EmailResult(EmailOutcome.Accepted, "<abc@smtp-relay.brevo.com>", null), result);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.Uri);
        Assert.Equal("test-key", handler.ApiKey);

        using var body = JsonDocument.Parse(handler.Body!);
        var root = body.RootElement;
        Assert.Equal("alerts@example.com", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("Klasly", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("parent@example.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Marites Mendoza", root.GetProperty("to")[0].GetProperty("name").GetString());
        Assert.Equal("Klasly: alert", root.GetProperty("subject").GetString());
        Assert.Equal("Hello", root.GetProperty("textContent").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, EmailOutcome.Permanent)]
    [InlineData(HttpStatusCode.NotFound, EmailOutcome.Permanent)]
    [InlineData(HttpStatusCode.Unauthorized, EmailOutcome.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, EmailOutcome.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests, EmailOutcome.Transient)]
    [InlineData(HttpStatusCode.InternalServerError, EmailOutcome.Transient)]
    [InlineData(HttpStatusCode.ServiceUnavailable, EmailOutcome.Transient)]
    public async Task Maps_each_error_status_to_an_outcome_with_brevos_message(HttpStatusCode status, EmailOutcome outcome)
    {
        var handler = new StubHandler(status, """{ "code": "x", "message": "brevo says no" }""");

        var result = await Client(handler).SendAsync("p@example.com", "P", "S", "T", Ct);

        Assert.Equal(outcome, result.Outcome);
        Assert.Null(result.MessageId);
        Assert.StartsWith(((int)status).ToString(), result.Error);
        Assert.Contains("brevo says no", result.Error);
    }

    [Fact]
    public async Task Accepted_with_an_unreadable_body_is_still_accepted()
    {
        // Brevo took the email; failing here would leave the row Pending and send it twice.
        var result = await Client(new StubHandler(HttpStatusCode.Created, "not json")).SendAsync("p@example.com", "P", "S", "T", Ct);

        Assert.Equal(EmailOutcome.Accepted, result.Outcome);
    }

    [Fact]
    public async Task Network_error_is_transient()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("no route to host"));

        var result = await Client(handler).SendAsync("p@example.com", "P", "S", "T", Ct);

        Assert.Equal(EmailOutcome.Transient, result.Outcome);
        Assert.Contains("no route to host", result.Error);
    }

    [Fact]
    public async Task Timeout_is_transient()
    {
        var handler = new StubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await Client(handler, TimeSpan.FromMilliseconds(100)).SendAsync("p@example.com", "P", "S", "T", Ct);

        Assert.Equal(new EmailResult(EmailOutcome.Transient, null, "timeout"), result);
    }

    [Fact]
    public async Task Cancelled_caller_still_throws()
    {
        // The Worker is shutting down mid-send: that must not be reported as a Brevo timeout.
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var handler = new StubHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Client(handler).SendAsync("p@example.com", "P", "S", "T", cts.Token));
    }

    [Theory]
    [InlineData("test-key", "alerts@example.com", true)]
    [InlineData(null, "alerts@example.com", false)]
    [InlineData("test-key", "", false)]
    public void Is_configured_only_with_a_key_and_a_sender(string? key, string sender, bool configured)
    {
        var client = new BrevoEmailClient(new HttpClient(), Options.Create(new BrevoOptions { ApiKey = key, SenderEmail = sender }));

        Assert.Equal(configured, client.IsConfigured);
    }

    // ---------- helpers ----------

    private static BrevoEmailClient Client(StubHandler handler, TimeSpan? timeout = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.brevo.com/v3/"), Timeout = timeout ?? TimeSpan.FromSeconds(10) },
            Options.Create(new BrevoOptions { ApiKey = "test-key", SenderEmail = "alerts@example.com", SenderName = "Klasly" }));

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public StubHandler(HttpStatusCode status, string body)
            : this((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) })) { }

        public HttpMethod? Method { get; private set; }
        public string? Uri { get; private set; }
        public string? ApiKey { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Method = request.Method;
            Uri = request.RequestUri?.ToString();
            ApiKey = request.Headers.TryGetValues("api-key", out var keys) ? keys.Single() : null;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return await respond(request, ct);
        }
    }
}
