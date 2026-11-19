# Tasks: Milestone 12 — Worker parent alerts outbox + Brevo email sender

> From [`plan-m12.md`](./plan-m12.md) and [`SPEC.md`](../SPEC.md). Every task ends with `dotnet build Klasly.slnx` + `dotnet test --solution Klasly.slnx`, one commit, and the three reports + an INDEX line (CLAUDE.md §5).

## Task 0: Housekeeping
- [x] SPEC.md marked Approved with the answers; `plan-m12.md` + this file written.

## Task 1: Brevo client (test first)

**What:**
- `src/Klasly.Worker/Email/BrevoEmailClient.cs`: `BrevoOptions`, `EmailOutcome`, `EmailResult`, `IBrevoEmailClient`, `BrevoEmailClient`;
- `Microsoft.Extensions.Http` 10.0.14 in `Klasly.Worker.csproj`;
- `Brevo:BaseUrl` / `SenderEmail` (empty) / `SenderName` in `appsettings.json`.

**Acceptance:**
- [x] The request is `POST smtp/email` with the `api-key` header and JSON `sender`, `to`, `subject`, `textContent`.
- [x] 201 `{ "messageId": "<x>" }` → Accepted + messageId. 400 → Permanent with Brevo's message. 401 and 403 → Unauthorized. 429, 500 and 503 → Transient.
- [x] A thrown `HttpRequestException` → Transient. A timeout (the handler waits past `HttpClient.Timeout`) → Transient "timeout". A cancelled caller token → still throws.
- [x] `IsConfigured` is false when ApiKey or SenderEmail is empty.

**Verify:** `dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.BrevoEmailClientTests`

**Files:** `src/Klasly.Worker/Email/BrevoEmailClient.cs`, `src/Klasly.Worker/Klasly.Worker.csproj`, `src/Klasly.Worker/appsettings.json`, `tests/Klasly.Tests/BrevoEmailClientTests.cs`. **Depends on:** none.

## Task 2: Email sender (test first)

**What:**
- `INotificationRepository.ListDueEmailsAsync(nowUtc, take)` (tracked, `User.Guardian` included, oldest first);
- `src/Klasly.Worker/Services/EmailSender.cs`, with `SendDueAsync(nowUtc, ct)` → `(Sent, Retrying, Failed)`.

**Acceptance:**
- [x] Accepted → Status Accepted, Attempts 1, `ExternalMessageId` set. Permanent → Failed + LastError (≤ 500 chars).
- [x] Transient → stays Pending, Attempts 1, NextAttemptAt between now + 5 min and now + 6 min. The next transient (Attempts 2) → between now + 10 min and now + 11 min. The transient that brings Attempts to `MaxEmailAttempts` → Failed.
- [x] Unauthorized → the row is unchanged (Attempts 0, Pending), the rows after it aren't sent, and an error is logged.
- [x] Not configured → listed rows Failed "Brevo is not configured", with no send.
- [x] Rows with a future `NextAttemptAt`, Push rows, and Accepted/Failed rows are never sent. At most 50 sends per run.
- [x] Each row is saved right after its send (a fake client that throws on the 2nd row leaves the 1st Accepted).
- [x] `MaxEmailAttempts` missing or outside 1–20 → throws with the key in the message.

**Verify:** `dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.EmailSenderTests`

**Files:** `src/Klasly.Core/Interfaces/INotificationRepository.cs`, `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs`, `src/Klasly.Worker/Services/EmailSender.cs`, `tests/Klasly.Tests/EmailSenderTests.cs`. **Depends on:** Task 1.

## Checkpoint A
- [x] Build clean, all tests pass, no pending model changes; `dotnet list Klasly.slnx package` shows the new package at 10.0.14 and nothing else new.

## Task 3: Absence alerts (test first)

**What:**
- `AbsenceAlert` record in `src/Klasly.Core/DTOs/AlertCandidates.cs`;
- `INotificationRepository.ListAbsenceAlertsAsync(sinceUtc)`;
- `src/Klasly.Worker/Services/AlertQueuer.cs`: `QueueAsync(nowUtc, ct)` → `(Absence, AtRisk)`, absence part.

**Acceptance:**
- [x] An Absent record (Auto or Manual) in a Finalized session, marked within 48 h, for student 1 → one Email/Absence/Pending row for guardian 1's user, with DedupKey `absent:{SessionId}:1:1`.
- [x] The subject is `Klasly: <student name> was absent from <code>`. The body has the section and the Manila date/time of the session.
- [x] None for: Excused; a Closed session; `MarkedAt` older than 48 h; `ReceivesAlerts = 0`; an inactive guardian user; a Dropped enrollment; an inactive-term class. ("Student 6 has no guardian" was dropped: the seed links student 6 to guardian 3, so every seeded student has one.)
- [x] Running twice → no new rows.

**Verify:** `dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.AlertQueuerTests`

**Files:** `src/Klasly.Core/DTOs/AlertCandidates.cs`, `src/Klasly.Core/Interfaces/INotificationRepository.cs`, `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs`, `src/Klasly.Worker/Services/AlertQueuer.cs`, `tests/Klasly.Tests/AlertQueuerTests.cs`. **Depends on:** none (can run before Task 1–2).

## Task 4: At-risk alerts (test first)

**What:** an `AtRiskAlert` record, `ListAtRiskAlertsAsync()`, and the at-risk part of `AlertQueuer`.

**Acceptance:**
- [x] The only snapshot is at-risk → one Email/AtRisk row per opted-in guardian, with DedupKey `atrisk:{RiskFlagId}:{GuardianId}`.
- [x] Not at-risk → at-risk → one row. At-risk → at-risk (latest) → none. At-risk → not → at-risk → one more row (new RiskFlagId).
- [x] The body shows the rate as a percent, the average or "no grades yet", and the early-warning sentence.
- [x] Running twice → no new rows. Dropped and inactive-term enrollments → none.

**Verify:** `dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.AlertQueuerTests`

**Files:** `src/Klasly.Core/DTOs/AlertCandidates.cs`, `src/Klasly.Core/Interfaces/INotificationRepository.cs`, `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs`, `src/Klasly.Worker/Services/AlertQueuer.cs`, `tests/Klasly.Tests/AlertQueuerTests.cs`. **Depends on:** Task 3.

## Task 5: Wire into the tick + smoke run without Brevo

**What:**
- `WorkerTickJob` calls `alerts.QueueAsync(now)` then `email.SendDueAsync(now)` after risk, with log lines;
- `Program.cs` registers `Configure<BrevoOptions>`, `AddHttpClient<IBrevoEmailClient, BrevoEmailClient>` (BaseAddress, 10 s), `AlertQueuer` and `EmailSender`;
- the placeholder comment is removed.

**Acceptance:**
- [x] Build and all tests pass.
- [x] Smoke on the dev database with no Brevo key, one tick:
  - the log shows queued counts and "Brevo is not configured";
  - the new Email rows are Failed with that error;
  - the next tick queues nothing new.
- [x] The report lists the rows created.

**Files:** `src/Klasly.Worker/Jobs/WorkerTickJob.cs`, `src/Klasly.Worker/Program.cs`. **Depends on:** Tasks 2, 4.

## Checkpoint B
- [x] Every SPEC.md success criterion met except the real-Brevo smoke. Microsoft.* 10.0.14, one new package, no migration, no secrets committed. Reports + INDEX.

## Task 6: Brevo setup + real email (team)
- [x] A free Brevo account; one verified sender address; an API key.
- [x] `dotnet user-secrets set "Brevo:ApiKey" "<key>" --project src/Klasly.Worker`; `Brevo:SenderEmail` set in `appsettings.Development.json` (or user-secrets if the team prefers not to commit it).
- [x] Every dev guardian's `ContactEmail` set to a team member's own address (dev database only, never seed data). The dev database has 4 guardians; any left on `@klasly.test` would bounce once the key works. Step-by-step: `docs/reports/2026-11-11-0918-milestone-12-review-fixes/HOW-TO-USE.md`.
- [x] Mark that guardian's child absent in an ended session → the next tick sends. The row is Accepted with a messageId and the email arrives.
- [x] With a wrong key: the log shows the 401 error and the row stays Pending.
