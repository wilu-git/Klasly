# Implementation Plan: Milestone 12 — Worker parent alerts outbox + Brevo email sender

> **Draft — waiting for team approval.** Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-11). Task details and acceptance criteria are in [`todo-m12.md`](./todo-m12.md). Milestone 11 is in `plan-m11.md` / `todo-m11.md` (complete). Milestone 10's Task 9 (Firebase) is still open and doesn't block this.

## Context

Milestone 11 is committed (`9d7dacb`). Milestone 12 implements guide §27.9 #9: queue absence and at-risk emails to linked guardians and send them through Brevo, retrying transient failures and never sending the same alert twice. Two new steps go at the end of `WorkerTickJob`.

**Decisions (spec open questions, 2026-11-11):**
- Q1: add `Microsoft.Extensions.Http` 10.0.14 to the Worker.
- Q2: 401/403 stops the batch; rows stay Pending with no attempt counted; error logged.
- Q3: absence alerts only for records with `MarkedAt` in the last 48 h.
- Q4: Brevo not configured → due rows Failed "Brevo is not configured".
- Q5: the Brevo account setup is the last task, done by the team.
- Q6: at most 50 sends per tick, as a constant.

## Reuse (don't rebuild)

- **HTTP client shape:** `src/Klasly.API/Push/FcmPushClient.cs`. A result record, a typed client, a 10 s timeout set at registration, and HTTP errors returned as a result, not thrown.
- **Sender shape:** `PushNotificationSender`: `Attempts++`, then `Accepted` + `ExternalMessageId` or `Failed` + `LastError`.
- **Settings reading with ranges:** `RiskCalculator.ReadDecimalAsync`. Copy the same small private helper for `MaxEmailAttempts` (1–20). `TimeZoneId` is read as a string and resolved with `TimeZoneInfo.FindSystemTimeZoneById`.
- **Repository:** extend the existing `INotificationRepository` / `NotificationRepository` (they already have `Add` and `SaveChangesAsync`).
- **Tests:**
  - `DatabaseFixture`, `TestData.NewClassAsync` (term 1 is active; the seed links students 1–5 to guardians 1–3);
  - the `FakeFcm` pattern in `PushTests`;
  - an xUnit collection, as Milestone 11 used for "Risk".

## Design notes

- **`BrevoEmailClient`** (`src/Klasly.Worker/Email/`):
  - `BrevoOptions { BaseUrl, ApiKey, SenderEmail, SenderName }` bound from `Brevo`; `IsConfigured` = ApiKey and SenderEmail both set.
  - `SendAsync(toEmail, toName, subject, text, ct)` → `EmailResult(EmailOutcome Outcome, string? MessageId, string? Error)`, where `EmailOutcome` is `Accepted | Permanent | Transient | Unauthorized`.
  - Status mapping: 2xx → Accepted (reads `messageId`); 401/403 → Unauthorized; 429 or ≥ 500 → Transient; any other status → Permanent.
  - `HttpRequestException`, and a timeout (an `OperationCanceledException` while the caller's token is *not* cancelled), → Transient.
  - Registered with `AddHttpClient<IBrevoEmailClient, BrevoEmailClient>`: BaseAddress from options, `Timeout = 10 s`.
- **`EmailSender.SendDueAsync(nowUtc, ct)`** → `(Sent, Retrying, Failed)`:
  1. Read `MaxEmailAttempts`.
  2. `ListDueEmailsAsync(now, 50)` returns tracked rows (Channel Email, Status Pending, `NextAttemptAt` null or ≤ now, oldest first) with `User.Guardian` loaded.
  3. If not configured → every listed row is set to `Failed` "Brevo is not configured" (Attempts unchanged); save; return.
  4. Otherwise, for each row:
     - no guardian email → Failed "No guardian email";
     - else send; on `Unauthorized` → log an error and **stop** (row untouched);
     - else `Attempts++` and apply spec assumption 8;
     - **save after each row**.
  
  Backoff: `now + 5 min × 2^(Attempts−1) + Random 0–60 s`; when `Attempts ≥ max` → Failed.
- **Alert queries** (`NotificationRepository`):
  - `ListAbsenceAlertsAsync(sinceUtc)`. Absent records with `MarkedAt ≥ since` in Finalized sessions of active-term classes, where the student's enrollment in that class is Active, joined to `StudentGuardians` (ReceivesAlerts, guardian user active). Rows whose DedupKey already exists are dropped.
  - `ListAtRiskAlertsAsync()`. Active enrollments in the active term whose latest RiskFlag is at-risk and whose second-latest is missing or not at-risk (`OrderByDescending(ComputedAt).ThenByDescending(RiskFlagId)`, `FirstOrDefault` / `Skip(1).FirstOrDefault`), joined to guardians the same way. Existing DedupKeys are dropped.
  - DedupKeys are built in SQL (`"absent:" + SessionId + ":" + …`) so the "already queued" check stays in the query. Fallback if EF can't translate it: return the candidates, then remove existing keys with one `WHERE DedupKey IN (…)` query.
- **`AlertQueuer.QueueAsync(nowUtc, ct)`** → `(Absence, AtRisk)`:
  1. Read `TimeZoneId`.
  2. List both kinds with `since = now − 48 h`.
  3. Build Subject/Body (spec assumption 6). Manila time is formatted like `Tue, 10 Nov 2026, 9:00–10:30 AM`; the rate as `25%`; the average as `72.50` or "no grades yet".
  4. `Add` Email / Pending rows (`UserId` = guardian user).
  5. One save.
- **Job:** `… → risk → alerts.QueueAsync(now) → email.SendDueAsync(now)`, each followed by one log line.
- **Tests share the xUnit collection "Outbox"** (`AlertQueuerTests`, `EmailSenderTests`): both read and change global Email rows. `PushTests` only touches Push rows, so it stays out.

## Task list

### Phase 1: Sending
- [ ] Task 1: `BrevoEmailClient` + options + package + appsettings, tested with a fake handler (M)
- [ ] Task 2: due-email query + `EmailSender` (results, backoff, max attempts, 401 stop, not configured), tested with a fake client (M)

### Checkpoint A
- [ ] Build clean, all tests pass, `dotnet list package` shows Microsoft.* 10.0.14 only, no model changes

### Phase 2: Queueing
- [ ] Task 3: absence alerts (query + `AlertQueuer` absence part + tests) (M)
- [ ] Task 4: at-risk alerts (query + `AlertQueuer` at-risk part + tests) (S)

### Phase 3: Wiring
- [ ] Task 5: wire both steps into `WorkerTickJob` + `Program.cs`; dev smoke run without Brevo (S)

### Checkpoint B
- [ ] Every spec criterion met except the real-Brevo smoke

### Phase 4: Team setup
- [ ] Task 6: Brevo account, verified sender, `Brevo:ApiKey` user-secret; real email smoke (team)

## Risks and mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| EF can't translate the DedupKey concat or the `Skip(1)` previous-snapshot subquery | Med | The fallbacks are in the design notes. Task 3/4 tests prove the result either way; the report says which shape was used. |
| The dev smoke run (Task 5) queues at-risk alerts for the 10 already-at-risk dev enrollments and, with no Brevo key, marks them Failed. Those alerts then never go out once Brevo is set up (DedupKey). | Low (dev data only) | Expected under Q4. Task 6's smoke uses a fresh absence on a guardian pointed at a team member's own address. The report lists the rows the smoke created. |
| Sending real email to the seeded `@klasly.test` addresses | Med (sender reputation) | Task 6 first sets one dev guardian's `ContactEmail` to a team member's address; tests never call the real client. |
| A slow Brevo makes the tick long | Low | 10 s timeout × 50 cap = 500 s, under the 600 s concurrency lock. |
| Tests running in parallel change global Email rows | Low | The "Outbox" collection; tests check their own rows (or the fake client's call count). |
| Timezone id "Asia/Manila" not found on a machine | Low | .NET 10 on Windows resolves IANA ids through ICU. A missing id throws in the queue step (logged, next tick retries); a test covers formatting. |

## Out of scope

- Admin page for Failed emails; HTML/Brevo templates; daily summary emails; SMS.
- Push rows (owned by the Web API).
- Deleting or archiving old Notifications.

## Open questions

None for building. Task 6 needs the team (Brevo account).
