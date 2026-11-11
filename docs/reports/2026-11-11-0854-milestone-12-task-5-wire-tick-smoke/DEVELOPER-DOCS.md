# Milestone 12 Task 5 — Alerts and emails in the Worker tick + smoke run

## Summary
`/build` Task 5 of `tasks/todo-m12.md`. `WorkerTickJob` now runs all five steps in the order CLAUDE.md §3.5 sets: close sessions → mark Absent and finalize → risk → **queue alerts** → **send emails**. `Program.cs` registers the alert queuer, the email sender and the typed Brevo client (base address from config, 10 s timeout). A smoke run on the dev `Klasly` database with no Brevo key queued 108 alerts on the first tick and nothing after. It marked every alert Failed "Brevo is not configured", 50 per tick, as spec Q4 says. Checkpoint B passes. Only Task 6 (a real Brevo account and email, by the team) is left in Milestone 12.

## Projects touched
CONSOLE WORKER, MSSQL (data written by the smoke run; no schema change).

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Worker/Jobs/WorkerTickJob.cs` | modified | Steps 4 and 5 with log lines; a warning when Brevo isn't configured; placeholder comment removed. |
| `src/Klasly.Worker/Program.cs` | modified | `AlertQueuer`, `EmailSender`, `Configure<BrevoOptions>`, `AddHttpClient<IBrevoEmailClient, BrevoEmailClient>`. |
| `SPEC.md` | modified | Success criteria ticked, except the real Brevo send (Task 6). |
| `tasks/plan-m12.md`, `tasks/todo-m12.md` | modified | Tasks 1–5 and Checkpoints A and B ticked. |
| `docs/reports/…` + `INDEX.md` | added / modified | These reports. |

## Key code
```csharp
// WorkerTickJob.RunAsync, after the risk step
var (absence, atRiskAlerts) = await alerts.QueueAsync(now, ct);
logger.LogInformation("Tick {UtcNow:u}: queued {Absence} absence and {AtRisk} at-risk emails", ...);

if (!brevo.IsConfigured)
    logger.LogWarning("Brevo is not configured (Brevo:ApiKey / Brevo:SenderEmail): due emails are marked Failed");
var (sent, retrying, failed) = await email.SendDueAsync(now, ct);
logger.LogInformation("Tick {UtcNow:u}: emails sent {Sent}, retrying {Retrying}, failed {Failed}", ...);
```
```csharp
// Program.cs
builder.Services.Configure<BrevoOptions>(builder.Configuration.GetSection("Brevo"));
builder.Services.AddHttpClient<IBrevoEmailClient, BrevoEmailClient>((sp, http) =>
{
    http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<BrevoOptions>>().Value.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(10); // 50 sends × 10 s stays under the job's 600 s lock
});
```
- **Order:** queueing runs after risk, so a snapshot that became at-risk this tick is alerted this tick. Sending runs right after queueing, so a new alert can go out in the same tick.
- **Brevo not configured:** the job only logs a warning; `EmailSender` does the marking. That makes it visible in the log, which the spec's smoke criterion asks for.

## Database changes
No migration (`has-pending-model-changes` → none). The smoke run **wrote these rows to the dev database**:

| Rows | NotificationId | Status | LastError |
| --- | --- | --- | --- |
| 98 Email / Absence | 7–104 | Failed | Brevo is not configured |
| 10 Email / AtRisk | 105–114 | Failed | Brevo is not configured |

By guardian: Marites Mendoza 32 absence + 5 at-risk; Rodel Dimaano 33 + 3; Linda Bautista 33 + 2. `Attempts` stayed 0 on every row, no DedupKey appears twice, and the 6 existing Push rows weren't touched.

## How it was verified
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **99 passed**.
- `dotnet user-secrets list --project src/Klasly.Worker` → no secrets, so Brevo is unconfigured as the smoke run needs.
- **Smoke run:** `dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"` for 3.5 minutes:

  | Tick | Queued | Emails | Log |
  | --- | --- | --- | --- |
  | 1 | 98 absence, 10 at-risk | failed 50 | "Brevo is not configured" warning |
  | 2 | 0 | failed 50 | warning |
  | 3 | 0 | failed 8 | warning |

  The 50-per-tick cap shows: 108 rows took three ticks. The risk step stayed at 0 new snapshots, and the other steps ran normally.
- Read back from SQL: the subjects and bodies match spec assumption 6. For example, `atrisk:1:1` "Klasly: Aira Mendoza may need support in ELECT3", with "Absence rate: 75%" and "Weighted average: 90.00". The time range in `absent:1019:6:3` reads "6:08 PM–6:11 PM (Asia/Manila)". I checked that the dash is stored as U+2013; sqlcmd only displays it as "-".
- Checkpoint B:
  - every Microsoft.* package is at 10.0.14, and `Microsoft.Extensions.Http` is the only package this milestone added;
  - no migration;
  - `git grep` finds no API key in `src`; `Brevo:ApiKey` isn't in any appsettings file;
  - the only email addresses are the seeded `@klasly.test` ones.

## Known limits / TODO
- **As the plan's risk table expected,** the 108 dev alerts are now Failed and will never be sent, even after Brevo is set up: the DedupKey stops them from being queued again. Task 6 must test with a **new** absence for a guardian whose `ContactEmail` points to a team member's own address.
- I didn't check that Hangfire logs a tick that throws (for example, a bad `TimeZoneId`); this is still open from Milestone 11. The services' throw-before-write behaviour is covered by tests.
- **Task 6 (team):**
  - a Brevo account with a verified sender;
  - `dotnet user-secrets set "Brevo:ApiKey" "<key>" --project src/Klasly.Worker`, plus `Brevo:SenderEmail`;
  - one real email, and a wrong-key run that leaves the row Pending.
