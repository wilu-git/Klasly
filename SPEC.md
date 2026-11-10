# Spec: Milestone 12 — Worker: parent alerts outbox + Brevo email sender

> Status: **Approved** (2026-11-11). Open questions 1–6 answered as recommended:
> - add `Microsoft.Extensions.Http` 10.0.14;
> - 401/403 stops the batch and leaves rows Pending;
> - absence alerts reach back 48 h;
> - unconfigured Brevo → Failed "Brevo is not configured";
> - Brevo account setup is the last task;
> - 50 emails per tick (constant).
>
> Plan: `tasks/plan-m12.md` + `tasks/todo-m12.md`.
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.8 "Email outbox (Brevo)" and "Risk calculation", §27.6 Notifications / Guardians / StudentGuardians / SystemSettings, §27.9 #9, §13 `BrevoEmailClient` + 10 s timeout, §14 `Brevo:ApiKey` and config) → `CLAUDE.md` (§3.5 Console Worker, §6 Milestone 12) → this spec.
> Milestone 11's spec is in git history (commit `9d7dacb`) and in `docs/reports/2026-11-11-0055-milestone-11-worker-risk-flags/`.

## What exists today

- **The outbox table is ready.** `Notifications` has every column (Channel, Type, Subject, Body, Status, Attempts, NextAttemptAt, LastError, DedupKey, ExternalMessageId, CreatedAt), plus:
  - CHECKs on the Channel, Type, Status and Attempts values;
  - a unique `DedupKey` (150 chars);
  - the `(Status, NextAttemptAt)` index.
  
  The Web API already writes and sends `Push / GradePosted` rows (Milestone 10). Nothing writes `Email` rows yet.
- **Recipients exist.** `Guardians.ContactEmail` (required), `StudentGuardians.ReceivesAlerts` (default true). The seed links students 1–5 to 3 fictional guardians at `@klasly.test`; student 6 has no guardian.
- **Triggers exist:**
  - Absent records from the Worker (`Source = Auto`) or a teacher correction (`Source = Manual`, `MarkedAt` = correction time);
  - `RiskFlags` snapshots written only on change (Milestone 11), so "became at-risk" = the latest row is at-risk and the row before it is not (or there is none).
- **Settings:** `MaxEmailAttempts = 5`, `TimeZoneId = Asia/Manila` are seeded.
- **The Worker:** `WorkerTickJob` runs close → Absent + finalize → risk, with a `// Milestone 12` placeholder. It has `[DisableConcurrentExecution]` and `[AutomaticRetry(Attempts = 0)]` (no Hangfire retries; the outbox retries).
- **Pattern to copy:** the API's `IFcmPushClient` / `FcmPushClient` (typed `HttpClient`, 10 s timeout, returns a result, never throws on HTTP errors) and `PushTests` (fake client).
- **Missing:** the Worker has **no `AddHttpClient`**. The Worker SDK doesn't include `Microsoft.Extensions.Http` (see Q1).

## Assumptions (correct these before we build)

1. **Two new steps, in this order, after risk:** **queue alerts** → **send emails**. Each is its own service; the job only calls them (CLAUDE.md §3.5).
2. **Who gets an alert:** each guardian linked to the student with `ReceivesAlerts = 1` whose user is active (`Users.IsActive = 1`). Only for **Active** enrollments in classes of the **active term**. One email per guardian per alert; a guardian with two children gets separate emails.
3. **Absence alert:**
   - **Trigger:** an `Absent` record in a **Finalized** session, with `MarkedAt` inside the last **48 hours** (see Q3).
   - **DedupKey:** `absent:{SessionId}:{StudentId}:{GuardianId}` (the guide's format).
   - **Corrections:** a teacher correcting Present → Absent sets `MarkedAt` to now, so it is picked up on the next tick. Correcting back to Present after the email is queued doesn't take it back.
   - **Excused:** never alerts.
4. **At-risk alert:**
   - **Trigger:** the enrollment's **latest** `RiskFlags` row is at-risk and the row before it is not at-risk or missing.
   - **DedupKey:** `atrisk:{RiskFlagId}:{GuardianId}`.
   - **Repeats:** a student who recovers and becomes at-risk again gets a new snapshot, so a new alert. A student still at-risk gets no repeat.
5. **"Never twice":** the queue query skips alerts whose DedupKey already exists. The unique index is the backstop. The Worker is the only writer and ticks never overlap, so there is no race.
6. **Email text** is stored in `Subject` / `Body` when queued, with times in `TimeZoneId` (Asia/Manila). Sent as Brevo `textContent` (plain text, no HTML template).
   - Absence: subject `Klasly: Aira Santos was absent from ELECT3`, body: who, subject + section, the session date and time (Manila), and that the teacher marked or confirmed the absence.
   - At-risk: subject `Klasly: Aira Santos may need support in ELECT3`, body: absence rate (%), weighted average (or "no grades yet"), and "This is an early-warning indicator for review, not a final grade" (§27.5).
7. **Sending:**
   - **Which rows:** `Channel = Email`, `Status = Pending`, `NextAttemptAt` null or ≤ now, oldest first, at most **50 per tick**. Push rows are never touched (the API owns them).
   - **Recipient:** read at send time from the guardian's `ContactEmail`.
   - **The call:** `POST {Brevo:BaseUrl}smtp/email` with the `api-key` header, JSON `sender {name, email}`, `to [{email, name}]`, `subject`, `textContent`; 10 s timeout.
8. **Results (guide §27.8):** every try does `Attempts++`, then:
   - **2xx** → `Accepted` + `ExternalMessageId` = Brevo `messageId`.
   - **400** and any other 4xx except 401/403/429 → `Failed` + `LastError` (status + Brevo's message, cut to 500 chars).
   - **429, 5xx, timeout, network error** → transient:
     - stays `Pending`;
     - `NextAttemptAt = now + 5 min × 2^(Attempts − 1)` (5, 10, 20, 40 min…), plus a random 0–60 s jitter;
     - when `Attempts ≥ MaxEmailAttempts` → `Failed` + LastError.
   - **401 / 403** → see Q2.
9. **Configuration:**
   - `appsettings.json`: `Brevo:BaseUrl` (`https://api.brevo.com/v3/`), `Brevo:SenderEmail`, `Brevo:SenderName` (`Klasly`).
   - User-secrets: `Brevo:ApiKey` only (§14).
   - `MaxEmailAttempts` is read from `SystemSettings` with the same "missing or out of range fails the step" rule as Milestone 11 (range 1–20).
10. **No schema change.** No API endpoint and no page. "Failed rows shown on the admin dashboard" (§27.8) waits for the dashboards; until then they are visible in the table and the Worker log.
11. **Saving:** the queue step saves once per tick; the send step saves **after each email**, so a crash never re-sends an already Accepted email.

## Objective

Parents hear about problems early. When their child is marked absent, or newly flagged at-risk, each linked guardian who wants alerts gets **one** email through Brevo. A Brevo outage or rate limit delays it (with backoff) but never loses or duplicates it (§27.9 #9).

### User stories

- As a **parent**, I get an email the same day my child is marked absent, and when my child becomes at-risk, so I can act before the term ends.
- As a **parent**, I never get the same alert twice, even if the Worker restarts or Brevo is down for an hour.
- As the **team**, we can see in `Notifications` which emails were sent, are waiting to retry, or failed and why.

## Design

```
WorkerTickJob.RunAsync
  ├─ AttendanceFinalizer (M9) → RiskCalculator (M11)
  ├─ AlertQueuer.QueueAsync(now)               ← new
  │     1. read TimeZoneId
  │     2. INotificationRepository.ListAbsenceAlertsAsync(sinceUtc)   → candidates without an existing DedupKey
  │     3. INotificationRepository.ListAtRiskAlertsAsync()            → candidates without an existing DedupKey
  │     4. build Subject/Body, Add Notification (Email, Pending), one save
  └─ EmailSender.SendDueAsync(now)             ← new
        1. read MaxEmailAttempts; if Brevo isn't configured → Q4
        2. INotificationRepository.ListDueEmailsAsync(now, take: 50)  (tracked, with Guardian email)
        3. per row: IBrevoEmailClient.SendAsync → apply result (assumption 8) → save
```

`IBrevoEmailClient` / `BrevoEmailClient` (Worker, typed `HttpClient`) returns `EmailResult(Kind: Accepted | Permanent | Transient | Unauthorized, MessageId?, Error?)` and never throws for HTTP errors or timeouts (except cancellation).

## Commands

```
Build:    dotnet build Klasly.slnx
Test:     dotnet test --solution Klasly.slnx
Worker:   dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"
Secret:   dotnet user-secrets set "Brevo:ApiKey" "<key>" --project src/Klasly.Worker
Model:    dotnet ef migrations has-pending-model-changes -p src/Klasly.Infrastructure -s src/Klasly.API   (must say none)
Packages: dotnet list Klasly.slnx package   (Microsoft.* all 10.0.14)
Check:    SELECT TOP 20 NotificationId, Type, Status, Attempts, NextAttemptAt, LastError, DedupKey FROM Notifications WHERE Channel = 'Email' ORDER BY NotificationId DESC;
```

## Project structure (files this milestone touches)

| Path | New / changed | Why |
| --- | --- | --- |
| `src/Klasly.Core/DTOs/AlertCandidates.cs` | new | `AbsenceAlert` and `AtRiskAlert`: one alert to queue (guardian, student, subject, session or snapshot facts). Due emails are plain tracked `Notification` rows with `User.Guardian` loaded, so they need no DTO. |
| `src/Klasly.Core/Interfaces/INotificationRepository.cs` | changed | `ListAbsenceAlertsAsync`, `ListAtRiskAlertsAsync`, `ListDueEmailsAsync`. |
| `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs` | changed | The three queries. |
| `src/Klasly.Worker/Email/BrevoEmailClient.cs` | new | `IBrevoEmailClient`, `EmailResult`, `BrevoOptions`, the typed client. |
| `src/Klasly.Worker/Services/AlertQueuer.cs` | new | Builds and queues the email rows. |
| `src/Klasly.Worker/Services/EmailSender.cs` | new | Sends due rows, applies results, backoff + jitter. |
| `src/Klasly.Worker/Jobs/WorkerTickJob.cs` | changed | Two new steps + log lines. |
| `src/Klasly.Worker/Program.cs` | changed | Options, `AddHttpClient<IBrevoEmailClient, BrevoEmailClient>` (10 s), services. |
| `src/Klasly.Worker/Klasly.Worker.csproj` | changed | `Microsoft.Extensions.Http` 10.0.14 (Q1). |
| `src/Klasly.Worker/appsettings.json` | changed | `Brevo:BaseUrl`, `SenderEmail`, `SenderName`. |
| `tests/Klasly.Tests/AlertQueuerTests.cs` | new | LocalDB: triggers, filters, dedup, text. |
| `tests/Klasly.Tests/EmailSenderTests.cs` | new | Fake client: every result branch, backoff, max attempts, push rows untouched. |
| `tests/Klasly.Tests/BrevoEmailClientTests.cs` | new | Fake `HttpMessageHandler`: request shape, header, status → result mapping, timeout. |

## Code style

Same as Milestones 9–11: primary constructors, `now` passed in by the job, `CancellationToken` everywhere, the HTTP client never throws on an HTTP status. One small piece of the sender:

```csharp
static DateTime NextAttempt(DateTime nowUtc, int attempts) =>
    nowUtc.AddMinutes(5 * Math.Pow(2, attempts - 1)).AddSeconds(Random.Shared.Next(0, 61)); // backoff + jitter (§27.8)
```

## Testing strategy

- **`BrevoEmailClientTests`** (no network, fake handler):
  - the URL, the `api-key` header and the JSON body;
  - 201 → Accepted + messageId;
  - 400 → Permanent; 401/403 → Unauthorized;
  - 429 / 500 / 503 → Transient; a timeout → Transient.
- **`EmailSenderTests`** (LocalDB + fake client):
  - each result updates Status, Attempts, NextAttemptAt, LastError and ExternalMessageId as in assumption 8;
  - the 5th transient failure → Failed;
  - a row with `NextAttemptAt` in the future is skipped;
  - Push rows are never touched;
  - at most 50 per run;
  - Q2 and Q4 behaviour.
- **`AlertQueuerTests`** (LocalDB):
  - an Absent in a Finalized session → one row per opted-in guardian, with the right DedupKey, subject and Manila time;
  - `ReceivesAlerts = 0`, an inactive guardian user, Excused, a Closed session, Dropped, an inactive term, or older than 48 h → none;
  - running twice → no duplicates;
  - at-risk flip → alert; still at-risk → none; recover then flip again → a second alert.
- The alert/email test classes share one xUnit collection (they read and write global outbox rows), like the Milestone 11 "Risk" collection.
- **Smoke (manual, needs Q5 setup):**
  - set one dev guardian's `ContactEmail` to a team member's own address;
  - set `Brevo:ApiKey` and a verified `SenderEmail`;
  - run the Worker, mark that student absent, and receive the email;
  - check the row is `Accepted` with a messageId;
  - with a wrong key, check Q2's behaviour.

## Boundaries

- **Always:**
  - keep the rules in the services and HTTP in the client (§13);
  - store UTC, show Manila time only in the email text;
  - run build + tests;
  - write the reports.
- **Ask first:**
  - any schema change;
  - adding a package beyond Q1;
  - HTML templates or Brevo template ids;
  - SMS or other channels (Optional in §27.2);
  - an admin page for failed emails.
- **Never:**
  - commit the Brevo key or a real person's email address (RA 10173);
  - send to the seeded `@klasly.test` addresses from a real Brevo account (they bounce and hurt the sender's reputation);
  - touch Push rows;
  - let Hangfire retry the tick;
  - delete Notifications rows.

## Success criteria

- [ ] A new Absent in a Finalized session queues exactly one Email/Absence row per opted-in, active guardian. Running again adds nothing.
- [ ] A student who becomes at-risk queues one Email/AtRisk row per opted-in guardian. Staying at-risk adds nothing; recovering and flipping again adds one more.
- [ ] Pending emails are sent through Brevo. 2xx → Accepted with messageId; 400 → Failed; 429/5xx/timeout → retried with growing backoff + jitter and Failed after `MaxEmailAttempts`; 401/403 per Q2.
- [ ] Push rows are untouched; an email Accepted once is never sent again, even after a crash mid-batch.
- [ ] Without Brevo configured, the Worker still runs every other step, behaving as Q4 says.
- [ ] No migration; Microsoft.* all 10.x; only the Q1 package added; no secret or real email committed; all tests pass.
- [ ] Reports written; INDEX line added.

## Open questions (my recommendation first)

1. **Add `Microsoft.Extensions.Http` 10.0.14 to the Worker?** Guide §13 and CLAUDE.md §4.1 require a typed `HttpClient` from `IHttpClientFactory`, and the Worker SDK doesn't include it. The API gets it free from ASP.NET Core. **Recommend yes** (same 10.0.14 as the other Microsoft packages; no other new package). Alternative: one static `HttpClient`, against the guide.
2. **401 / 403 from Brevo.** The guide calls them permanent (Failed). But they mean *our* API key or account is wrong, not the parent's address, so every row in the batch would be marked Failed and never sent. **Recommend:**
   - stop the batch;
   - leave the rows Pending without counting an attempt;
   - log an error: "Brevo rejected the API key (401). Emails wait until it is fixed."
   
   Alternative: follow the guide literally (Failed).
3. **How far back do absence alerts reach?** Without a limit, the first run would email every Absent of the whole term (the dev database has dozens). **Recommend 48 hours by `MarkedAt`.** That covers a Worker outage up to 2 days, and older absences are never emailed. Alternatives: 24 h, or only records marked after this feature is first deployed.
4. **Brevo not configured** (no `Brevo:ApiKey` or `SenderEmail`). **Recommend: mark due rows `Failed` "Brevo is not configured",** the same as Milestone 10's "FCM is not configured". Nothing piles up and blasts out the day the key is added. Alternative: leave them Pending, skip sending, and log a warning.
5. **Brevo account setup** waits on the team, like Milestone 10's Firebase:
   - create a free Brevo account;
   - verify one sender address;
   - create an API key;
   - `dotnet user-secrets set "Brevo:ApiKey" …`;
   - put the sender in `appsettings.json` (it isn't a secret).
   
   **Recommend: the plan includes these as the last task, and every other task is tested with fakes so nothing blocks on it.**
6. **Per-tick cap of 50 emails.** It keeps a tick well under Hangfire's 10-minute lock even with slow responses, and inside Brevo's free 300/day. **Recommend 50, as a constant with a `shortcut:` comment** (move it to config if the school is bigger).
