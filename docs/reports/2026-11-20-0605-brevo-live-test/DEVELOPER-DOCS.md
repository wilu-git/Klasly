# Developer docs — Brevo live test (Milestone 12, Task 6)

## Summary
We put the real Brevo API key and verified sender into the Worker's user-secrets, then ran two live tests against Brevo's transactional API. **Test 1 (good key):** a Worker tick sent 2 absence emails; Brevo accepted both, and each row was saved as `Accepted` with Brevo's messageId. **Test 2 (wrong key):** Brevo answered 401, the Worker logged the error, and the row stayed `Pending` with no attempt counted. No code changed: the sender was built in Milestone 12 Tasks 1–5. All five Task 6 boxes in `tasks/todo-m12.md` are now ticked.

## Projects touched
- **CONSOLE WORKER**: configuration only (user-secrets), no code.
- **MSSQL**: data edits in the local dev database only (guardian contact emails, test rows reset). No schema change.

## Files changed
| Path | Change | Why |
|---|---|---|
| `tasks/todo-m12.md` | modified | Ticked all five Task 6 boxes (account + key, secrets, guardian emails, real send, wrong key). |
| `docs/reports/2026-11-20-0605-brevo-live-test/*` | added | This report. |
| `docs/reports/INDEX.md` | modified | Index line for this report. |

Not in git (kept out on purpose): `Brevo:ApiKey` and `Brevo:SenderEmail` live in the Worker's user-secrets (`%APPDATA%\Microsoft\UserSecrets\dotnet-Klasly.Worker-e51f76c3-…\secrets.json`).

## Key code (existing, exercised by the test)
- `src/Klasly.Worker/Program.cs`: binds the `Brevo` section and registers the typed client:
  ```csharp
  var brevoBaseUrl = BrevoOptions.ParseBaseUrl(builder.Configuration["Brevo:BaseUrl"]);
  builder.Services.Configure<BrevoOptions>(builder.Configuration.GetSection("Brevo"));
  builder.Services.AddHttpClient<IBrevoEmailClient, BrevoEmailClient>(http => { http.BaseAddress = brevoBaseUrl; ... });
  ```
- `src/Klasly.Worker/Email/BrevoEmailClient.cs`: `POST https://api.brevo.com/v3/smtp/email` and maps Brevo's answer to `EmailOutcome` (`Accepted` / `Permanent` / `Transient` / `Unauthorized`).
- `src/Klasly.Worker/Services/EmailSender.cs`: `SendDueAsync` takes due `Pending` email rows (max 50 per tick) and sends each one to the guardian's `ContactEmail`. On `Unauthorized` it stops and leaves every row Pending, with no attempt counted.

## Commands run

### Worker secrets (run by the developer, so the key never appears in chat or in git)
```bash
dotnet user-secrets set "Brevo:ApiKey" "<your-v3-api-key>" --project src/Klasly.Worker
dotnet user-secrets set "Brevo:SenderEmail" "<verified-sender@example.com>" --project src/Klasly.Worker
dotnet user-secrets list --project src/Klasly.Worker
```

### Dev-database prep (sqlcmd against `(localdb)\MSSQLLocalDB`, database `Klasly`)
```sql
-- Guardians 1–2 → team member A's inbox, guardians 3–4 → team member B's inbox (dev DB only, never seed data)
UPDATE Guardians SET ContactEmail = '<team-member-A@gmail.com>' WHERE GuardianId IN (1,2);
UPDATE Guardians SET ContactEmail = '<team-member-B@gmail.com>' WHERE GuardianId IN (3,4);
-- Re-queue two absence alerts that had failed earlier with "Brevo is not configured"
UPDATE Notifications SET Status='Pending', Attempts=0, NextAttemptAt=NULL, LastError=NULL WHERE NotificationId IN (100,104);
```

### Test 1: good key
```bash
dotnet run --project src/Klasly.Worker
```
The Hangfire job runs on the next 5-minute mark (`Worker:Cron = */5 * * * *`). Log from that tick:
```
Tick 2026-11-19 21:57:02Z: queued 0 absence and 0 at-risk emails
Sending HTTP request POST https://api.brevo.com/v3/smtp/email
Sending HTTP request POST https://api.brevo.com/v3/smtp/email
Tick 2026-11-19 21:57:02Z: emails sent 2, retrying 0, failed 0
```
Rows afterwards:
| NotificationId | Type | Status | Attempts | ExternalMessageId |
|---|---|---|---|---|
| 100 | Absence | Accepted | 1 | `<202611192157.48361798737@smtp-relay.mailin.fr>` |
| 104 | Absence | Accepted | 1 | `<202611192157.56495027328@smtp-relay.mailin.fr>` |

### Test 2: wrong key
```sql
UPDATE Notifications SET Status='Pending', Attempts=0, NextAttemptAt=NULL, LastError=NULL WHERE NotificationId = 102;
```
```bash
Brevo__ApiKey=xkeysib-wrong-key-for-test dotnet run --project src/Klasly.Worker
```
The environment variable overrides user-secrets for that one run only, so the real key is never touched. Log:
```
Received HTTP response headers after 1053.8894ms - 401
ERR Brevo refused our API key or account (401 {"message":"Key not found","code":"unauthorized"}). Emails wait until it is fixed.
Tick 2026-11-19 22:00:06Z: emails sent 0, retrying 0, failed 0
```
Row 102 afterwards: `Pending`, Attempts `0`, LastError `NULL`, as the spec requires. After the test it was set back to its earlier state (`Failed`, "Brevo is not configured"), so a later real run won't send an old alert.

## Database changes
None to the schema. Data only, local dev database: 4 guardian `ContactEmail` values, plus notification rows 100 and 104 (now Accepted) and row 102 (restored to Failed).

## How it was verified
- The Worker log for each tick (shown above) and a `SELECT` on `Notifications` after each tick.
- The Worker was stopped after each test.
- No `dotnet build`/`dotnet test` run: no code changed.

## Known limits / TODO
- "Accepted" means Brevo took the email. Arrival in the inbox was not checked from here; check the inbox (and spam).
- Test 1 re-sent old absence alerts. A fresh absence (session ends → non-scanner marked Absent → alert queued) was not created; the queueing step ran in the same tick and correctly queued nothing new.
- Each developer's dev database still has `@klasly.test` guardian emails until they change them; those would bounce once a key is set.
- The Brevo MCP server for Claude Code is separate from the Worker and was not set up in this task.
