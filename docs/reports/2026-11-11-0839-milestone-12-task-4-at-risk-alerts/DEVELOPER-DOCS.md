# Milestone 12 Task 4 — At-risk alerts

## Summary
`/build` Task 4 of `tasks/todo-m12.md`: `AlertQueuer` now also queues an email when an enrollment **becomes** at-risk (spec assumption 4). That means the latest RiskFlags snapshot is at-risk and the one before it is missing or not at-risk. Each opted-in guardian with an active account gets one email per such snapshot, never twice. A student who stays at-risk gets no repeat; one who recovers and becomes at-risk again gets a new email. The step isn't in `WorkerTickJob` yet (Task 5).

## Projects touched
CONSOLE WORKER, EF CORE (a new query; no schema change), and the test project.

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Core/DTOs/AlertCandidates.cs` | modified | The `AtRiskAlert` record. |
| `src/Klasly.Core/Interfaces/INotificationRepository.cs` | modified | `ListAtRiskAlertsAsync(ct)`. |
| `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs` | modified | The query. |
| `src/Klasly.Worker/Services/AlertQueuer.cs` | modified | The at-risk part; `QueueAsync` now returns the real `AtRisk` count. |
| `tests/Klasly.Tests/AlertQueuerTests.cs` | modified | 7 new tests (15 in the class). The `QueueAsync` helper now returns the counts. |
| `tasks/todo-m12.md` | modified | Task 4 ticked. |
| `docs/reports/…` + `INDEX.md` | added / modified | These reports. |

## Key code
```csharp
from e in context.Enrollments
where e.Status == EnrollmentStatus.Active && e.Class.Term.IsActive
let latest = e.RiskFlags.OrderByDescending(f => f.ComputedAt).ThenByDescending(f => f.RiskFlagId).FirstOrDefault()
let previousAtRisk = e.RiskFlags.OrderByDescending(f => f.ComputedAt).ThenByDescending(f => f.RiskFlagId)
    .Skip(1).Select(f => (bool?)f.IsAtRisk).FirstOrDefault()
where latest != null && latest.IsAtRisk && previousAtRisk != true // newly at-risk: no repeat while it stays so
from sg in e.Student.StudentGuardians
where sg.ReceivesAlerts && sg.Guardian.User.IsActive
let key = "atrisk:" + latest.RiskFlagId + ":" + sg.GuardianId
where !context.Notifications.Any(n => n.DedupKey == key)
```
- **The "latest" order matches Milestone 11** (`ComputedAt` desc, then `RiskFlagId` desc), so the alert and the risk step agree on which row is latest.
- **The plan's risk "EF can't translate `Skip(1)`" didn't happen.** The query is one SQL command (`OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY`), so no fallback was needed. EF repeats the "latest flag" `TOP(1)` subquery about six times; each is a seek on the existing `(EnrollmentId, ComputedAt)` index.
- **Email text** (plain text, for Brevo `textContent`):
  ```
  Subject: Klasly: Aira Mendoza may need support in ELECT3

  Good day, Marites Mendoza.

  Aira Mendoza may need support in ELECT3 Mobile Application Development, section A.

  Absence rate: 25%
  Weighted average: 72.50          (or: no grades yet)

  This is an early-warning indicator for review, not a final grade. Please talk with Aira Mendoza and the teacher.

  Klasly
  ```
  The rate is `AbsenceRate × 100` with up to 2 decimals (`33.33%`). The average uses 2 decimals, the column's scale.

## Database changes
None.

## How it was verified
- RED: the 7 new tests were written first. 5 failed; the 2 "nothing is queued" tests passed because nothing was queued yet, as expected.
- GREEN: `--filter-class Klasly.Tests.AlertQueuerTests` → 15 passed.
- Mutation check: I broke the query 6 ways (backed up to the scratchpad, restored, confirmed with `diff`):

  | Break | Result |
  | --- | --- |
  | Ignore the previous flag | caught |
  | Oldest flag instead of latest | caught |
  | No Active-enrollment filter | caught |
  | No active-term filter | caught |
  | No DedupKey check | caught |
  | Don't require the latest flag to be at-risk | **not caught at first** |

  For the one that slipped through, I added `A_safe_latest_snapshot_queues_nothing` (only a safe snapshot, and at-risk → recovered). That break is now caught.
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **99 passed** (92 + 7), run three times with no flaky failures.

## Known limits / TODO
- **Why the test flags are dated tomorrow:** `RiskCalculatorTests` runs in parallel (another collection) and snapshots every active enrollment at the real time. If one of those snapshots became the latest row of a test enrollment, the test would break. Dating the test flags in the future keeps them the latest rows.
- **The dev smoke run in Task 5** will queue at-risk emails for the 10 dev enrollments that are already at-risk. Without a Brevo key, they become Failed "Brevo is not configured" and won't be sent later (plan risk table).
- **A student who drops the class and re-enrolls** has a new enrollment, so its first at-risk snapshot alerts again.
