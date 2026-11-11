# Milestone 12 Task 2 — Email sender

## Summary
Continued `tasks/todo-m12.md` with Task 2: the Worker step that sends due Email rows from the Notifications outbox through `IBrevoEmailClient` (Task 1) and records each result (guide §27.8, spec assumption 8). It covers every outcome: Accepted, Permanent, Transient with backoff + jitter until `MaxEmailAttempts`, Unauthorized (stop the batch, spec Q2), and Brevo not configured (spec Q4). It saves after every row and sends at most 50 per run. Checkpoint A also passes. The step isn't in `WorkerTickJob` yet (Task 5).

## Projects touched
CONSOLE WORKER, EF CORE (a new query; no schema change), and the test project.

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Core/Interfaces/INotificationRepository.cs` | modified | `ListDueEmailsAsync(nowUtc, take, ct)`. |
| `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs` | modified | The due-email query. |
| `src/Klasly.Worker/Services/EmailSender.cs` | added | `SendDueAsync(nowUtc, ct)` → `(Sent, Retrying, Failed)`. |
| `tests/Klasly.Tests/EmailSenderTests.cs` | added | 13 tests (10 facts + a 4-case theory) with a fake Brevo, in the `"Outbox"` collection. |
| `tasks/todo-m12.md` | modified | Task 2 and Checkpoint A ticked. |
| `docs/reports/…` + `INDEX.md` | added / modified | These reports. |

## Key code
The query: tracked, with the guardian's address loaded, oldest first.
```csharp
context.Notifications
    .Include(n => n.User).ThenInclude(u => u.Guardian)
    .Where(n => n.Channel == NotificationChannel.Email && n.Status == NotificationStatus.Pending
                && (n.NextAttemptAt == null || n.NextAttemptAt <= nowUtc))
    .OrderBy(n => n.NotificationId) // identity order = oldest first
    .Take(take)
```
The per-row result handling in `EmailSender`:
```csharp
if (result.Outcome == EmailOutcome.Unauthorized) { logger.LogError(...); break; } // row untouched, no attempt counted
n.Attempts++;
switch (result.Outcome)
{
    case EmailOutcome.Accepted: Status = Accepted; ExternalMessageId = messageId; break;
    case EmailOutcome.Transient when n.Attempts < maxAttempts:
        n.NextAttemptAt = nowUtc.AddMinutes(5 << (n.Attempts - 1)).AddSeconds(Random.Shared.Next(0, 61)); break;
    default: Fail(n, result.Error); break; // Permanent, or Transient with no attempts left
}
await notifications.SaveChangesAsync(ct); // per row
```
- **`MaxEmailAttempts`** is read first, and the step throws when it is missing or outside 1–20, the same rule as Milestone 11's settings.
- **Brevo not configured** → every listed row is set to Failed "Brevo is not configured" with Attempts unchanged, and there is no send.
- **A row whose user has no guardian profile** → Failed "No guardian email", with no send and no attempt counted. This is a guard from the plan's design notes: the alert queuer (Tasks 3–4) only writes rows for guardian users, so it should never happen.
- **`LastError`** is cut to the column's 500 chars (same as `PushNotificationSender`). Transient retries keep the latest error too, so the team can see why a row is waiting.
- **The 50 cap** is a constant with a `shortcut:` comment (spec Q6).
- `5 << (Attempts − 1)` is the spec's `5 × 2^(Attempts−1)` in whole minutes. It avoids `Math.Pow`'s double, and with Attempts ≤ 20 it can't overflow.

## Database changes
None. `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration."

## How it was verified
- RED: `dotnet build tests/Klasly.Tests` → CS0246 `EmailSender` not found.
- GREEN: `--filter-class Klasly.Tests.EmailSenderTests` → 13 passed.
- Mutation check: I broke the code 5 ways, one at a time (files backed up to the scratchpad, restored, and confirmed identical with `diff`):

  | Break | Result |
  | --- | --- |
  | One save at the end instead of per row | caught (1 failed) |
  | No `NextAttemptAt` filter | caught |
  | No `Channel == Email` filter | caught |
  | Backoff not doubling | caught |
  | Count the attempt before the 401 check | **not caught, and can't be:** the `break` skips the save, so the extra count never reaches the database. The two versions behave the same. |
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **84 passed** (71 + 13).
- Checkpoint A: `dotnet list Klasly.slnx package` shows every Microsoft.* package at 10.0.14, and `Microsoft.Extensions.Http` is the only new one. No pending model changes.

## Known limits / TODO
- Not wired in yet: Task 5 registers `EmailSender` and adds it to `WorkerTickJob` after the alert step.
- The tests share the database's Email rows, so they check only their own rows (by a unique subject) and run one at a time in the `"Outbox"` collection. Task 3's `AlertQueuerTests` must join that collection.
- The jitter uses `Random.Shared`, so the tests check a range (`now + 5 min` … `now + 6 min`), not an exact time.
