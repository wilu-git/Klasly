# Milestone 12 review fixes

## Summary
The Milestone 12 review found three issues, and the team asked to fix all three:
1. Brevo's **402** ("account requires activation or additional credits") and a 400 whose code is `not_enough_credits` or `account_under_validation` were treated as Permanent. That marked every due email Failed for good when the account ran out of credits, for example past the free plan's 300 a day.
2. A `Brevo:BaseUrl` without a trailing slash sent every request to `/smtp/email` (404 → Permanent). An empty or invalid value broke the whole tick, attendance steps included.
3. `WorkerTickJob` held a Brevo dependency only to log a warning, which goes against CLAUDE.md §3.5 (the job only calls services).

The user also asked for step-by-step Brevo registration and setup; it is in `HOW-TO-USE.md` here (it is Task 6).

## Projects touched
CONSOLE WORKER (and the test project).

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Worker/Email/BrevoEmailClient.cs` | modified | 402 and the two account codes → `Unauthorized`; `BrevoOptions.ParseBaseUrl`; `MessageIdFrom` generalized to `StringFrom(body, property)`. |
| `src/Klasly.Worker/Program.cs` | modified | Parses `Brevo:BaseUrl` once at startup and sets it as the client's base address. |
| `src/Klasly.Worker/Services/EmailSender.cs` | modified | Logs the "not configured" warning itself; the 401/402/403 error text now says "API key or account". |
| `src/Klasly.Worker/Jobs/WorkerTickJob.cs` | modified | `IBrevoEmailClient` and the warning removed; the job only calls services again. |
| `tests/Klasly.Tests/BrevoEmailClientTests.cs` | modified | 9 new cases: 402, three 400 codes, three base URLs that get normalized, three bad base URLs. |
| `tests/Klasly.Tests/EmailSenderTests.cs` | modified | The not-configured test also checks the warning. |
| `tasks/todo-m12.md` | modified | Task 6 now says to repoint **every** dev guardian (4 in the dev database), and links the setup guide. |

## Key code
```csharp
var outcome = status switch
{
    401 or 402 or 403 => EmailOutcome.Unauthorized,
    400 when StringFrom(body, "code") is "not_enough_credits" or "account_under_validation" => EmailOutcome.Unauthorized,
    429 or >= 500 => EmailOutcome.Transient,
    _ => EmailOutcome.Permanent
};
```
```csharp
public static Uri ParseBaseUrl(string? value) =>
    Uri.TryCreate((value ?? DefaultBaseUrl).TrimEnd('/') + "/", UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
        ? uri
        : throw new InvalidOperationException($"Setting 'Brevo:BaseUrl' is invalid: it must be an https URL, e.g. {DefaultBaseUrl}");
```
- `Unauthorized` keeps its name, but its doc comment now means "our key or account is wrong". The sender still stops the batch and leaves rows Pending with no attempt counted (spec Q2). So once credits return the next day, or the account is activated, the emails go out.
- **HTTPS only:** the `api-key` header must never travel unencrypted. An `http://` base URL now stops the Worker at startup.
- **Source for the status codes:** Brevo's "How it works" page lists 402 as "Account requires activation or additional credits". The `sendtransacemail` reference lists `not_enough_credits` among its 400 error codes. The docs don't say which status the free plan's daily limit returns, so both are handled.

## Database changes
None.

## How it was verified
- RED: the build failed with CS0117 `BrevoOptions` has no `ParseBaseUrl`. The new 402 and code cases were written before the mapping changed.
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **109 passed** (99 + 10).
- Mutation check (files backed up, restored, confirmed with `cmp`). Every break was caught:

  | Break | Result |
  | --- | --- |
  | Drop 402 | caught |
  | Drop the account-code check | caught (2 tests) |
  | Drop the slash fix | caught |
  | Drop the https check | caught |
  | Drop the warning | caught |
- Startup check: `dotnet run --project src/Klasly.Worker -- --Brevo:BaseUrl "http://api.brevo.com/v3/"` → `Unhandled exception. System.InvalidOperationException: Setting 'Brevo:BaseUrl' is invalid: it must be an https URL…`. It fails immediately, before any tick.

## Known limits / TODO
- **The duplicate risk is unchanged:** a timeout after Brevo accepted an email can still send it twice. Brevo can't tell us otherwise.
- **Opt-outs and deactivations still aren't rechecked before a retry.** I kept that out of scope; it's a review note, not a bug in the spec.
- **An unverified sender may still return a plain 400**, which the docs don't describe. The setup guide verifies the sender before the key is added.
- **Task 6 (team)** is still open: see `HOW-TO-USE.md`.
