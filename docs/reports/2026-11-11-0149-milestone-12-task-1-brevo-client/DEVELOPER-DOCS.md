# Milestone 12 Task 1 — Brevo email client

## Summary
`/build` picked Task 1 of `tasks/todo-m12.md`: the typed `HttpClient` that sends one plain-text email through Brevo's transactional API (guide §13). It sorts every answer into one of four outcomes the email sender (Task 2) will act on: Accepted, Permanent, Transient, Unauthorized. It never throws for HTTP errors, network errors or timeouts; only the caller's cancellation throws. The approved spec and plan were committed first as `2639e7f`. Nothing uses the client yet; it is registered in Task 5.

## Projects touched
CONSOLE WORKER (and the test project).

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Worker/Email/BrevoEmailClient.cs` | added | `BrevoOptions`, `EmailOutcome`, `EmailResult`, `IBrevoEmailClient`, `BrevoEmailClient`. |
| `src/Klasly.Worker/Klasly.Worker.csproj` | modified | `Microsoft.Extensions.Http` 10.0.14 (spec Q1) for `AddHttpClient` in Task 5. |
| `src/Klasly.Worker/appsettings.json` | modified | `Brevo:BaseUrl`, `SenderEmail` (empty until Task 6), `SenderName`. No key here: `Brevo:ApiKey` is a user-secret. |
| `tests/Klasly.Tests/BrevoEmailClientTests.cs` | added | 15 tests against a fake `HttpMessageHandler`. |
| `tasks/todo-m12.md` | modified | Task 1 ticked. |
| `docs/reports/…` + `INDEX.md` | added / modified | These reports. |

## Key code
```csharp
var outcome = status switch
{
    401 or 403    => EmailOutcome.Unauthorized, // our key/account is wrong (spec Q2)
    429 or >= 500 => EmailOutcome.Transient,
    _             => EmailOutcome.Permanent     // 400 and other 4xx
};
...
catch (HttpRequestException ex) { return new(EmailOutcome.Transient, null, $"network error: {ex.Message}"); }
catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(EmailOutcome.Transient, null, "timeout"); }
```
- **The request:** `POST smtp/email` (relative to `Brevo:BaseUrl`), header `api-key`, JSON `{ sender {name,email}, to [{email,name}], subject, textContent }`.
- **A 2xx with an unreadable body is still Accepted** (messageId "(no messageId)"). Treating it as a failure would leave the row Pending and send the email twice.
- **Errors keep Brevo's response body** (`"400 {…message…}"`). The sender (Task 2) cuts it to the 500-char `LastError` column.

## Database changes
None.

## How it was verified
- RED: `dotnet build tests/Klasly.Tests` → CS0234 "namespace 'Email' does not exist", CS0246 `BrevoEmailClient`.
- GREEN: `--filter-class Klasly.Tests.BrevoEmailClientTests` → 15 passed.
- The first version of the cancellation test passed for the wrong reason: its fake ignored the token, so nothing was cancelled. I rewrote it so the fake waits and the caller cancels after 100 ms. To prove it now works, I removed the `when (!ct.IsCancellationRequested)` filter; the test failed, and I restored the file.
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **71 passed** (56 + 15).
- `dotnet list Klasly.slnx package` → `Microsoft.Extensions.Http 10.0.14`, matching `Microsoft.Extensions.Hosting 10.0.14`; no other new package.

## Known limits / TODO
- The client isn't registered or used yet. Task 2 is the sender; Task 5 is `AddHttpClient<IBrevoEmailClient, BrevoEmailClient>` with the BaseAddress and the 10 s timeout.
- I haven't called the real Brevo API yet; that's Task 6, which needs the team's account.
