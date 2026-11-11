# Milestone 12 Task 3 — Absence alerts

## Summary
`/build` Task 3 of `tasks/todo-m12.md`: the Worker's alert step now turns recent absences into Pending Email rows in the Notifications outbox (spec assumptions 2, 3, 5, 6). Each row goes to one guardian who has alerts on and an active account. The text is written when the row is queued, with the session time in the `TimeZoneId` zone (Asia/Manila). The same alert is never queued twice. The at-risk half is Task 4, and the step isn't in `WorkerTickJob` yet (Task 5).

## Projects touched
CONSOLE WORKER, EF CORE (a new query; no schema change), and the test project.

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Core/DTOs/AlertCandidates.cs` | added | The `AbsenceAlert` record: one alert still to queue. Task 4 adds `AtRiskAlert` here. |
| `src/Klasly.Core/Interfaces/INotificationRepository.cs` | modified | `ListAbsenceAlertsAsync(sinceUtc, ct)`. |
| `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs` | modified | The query. |
| `src/Klasly.Worker/Services/AlertQueuer.cs` | added | `QueueAsync(nowUtc, ct)` → `(Absence, AtRisk)`; AtRisk is 0 until Task 4. |
| `tests/Klasly.Tests/AlertQueuerTests.cs` | added | 8 tests (6 facts/theories with cases), in the `"Outbox"` collection. |
| `tests/Klasly.Tests/EmailSenderTests.cs` | modified | The 50-cap test first sends what other tests left due (alerts queued by `AlertQueuerTests`), so its own rows aren't pushed past the cap. |
| `tasks/todo-m12.md` | modified | Task 3 ticked; one wrong acceptance case corrected (see Known limits). |
| `docs/reports/…` + `INDEX.md` | added / modified | These reports. |

## Key code
```csharp
from r in context.AttendanceRecords
where r.Status == AttendanceStatus.Absent && r.MarkedAt >= sinceUtc
      && r.Session.Status == SessionStatus.Finalized && r.Session.Class.Term.IsActive
      && r.Session.Class.Enrollments.Any(e => e.StudentId == r.StudentId && e.Status == EnrollmentStatus.Active)
from sg in r.Student.StudentGuardians
where sg.ReceivesAlerts && sg.Guardian.User.IsActive
let key = "absent:" + r.SessionId + ":" + r.StudentId + ":" + sg.GuardianId
where !context.Notifications.Any(n => n.DedupKey == key) // never queue the same alert twice
select new AbsenceAlert(key, ...)
```
- **The plan's risk "EF can't build the DedupKey in SQL" didn't happen.** EF writes one statement with `NOT EXISTS (… WHERE [n].[DedupKey] = N'absent:' + CAST(… AS nvarchar(150)) …)`. The cast matches the column, so the unique `DedupKey` index can be used, and the fallback isn't needed.
- **`AlertQueuer`** reads `TimeZoneId` first. When it is missing or unknown, it throws `InvalidOperationException` naming the key, before anything is queued. It looks back 48 hours (spec Q3, a constant), adds one row per alert (`Email`, `Absence`, `Pending`, `CreatedAt = now`), and saves once.
- **Email text** (plain, for Brevo `textContent`):
  ```
  Subject: Klasly: Aira Mendoza was absent from ELECT3

  Good day, Marites Mendoza.

  Aira Mendoza was marked absent from ELECT3 Mobile Application Development, section A, in the class on Tue, 10 Nov 2026, 9:00 AM–10:30 AM (Asia/Manila).

  The teacher marked or confirmed this absence in Klasly. If you have questions, please contact the teacher.

  Klasly
  ```

## Database changes
None. No migration; the query only reads existing tables and writes Notifications rows.

## How it was verified
- RED: `dotnet build tests/Klasly.Tests` → CS0246 `AlertQueuer` not found.
- GREEN: `--filter-class Klasly.Tests.AlertQueuerTests` → 8 passed.
- Mutation check: I removed each filter from the query, one at a time (backed up to the scratchpad, restored, confirmed with `diff`). Every break was caught:

  | Break | Result |
  | --- | --- |
  | Absent only | caught |
  | 48 h | caught |
  | Finalized | caught |
  | Active term | caught |
  | Active enrollment | caught |
  | `ReceivesAlerts` | caught |
  | Guardian account active | caught |
  | DedupKey check | caught (5 tests; the unique index throws on the second run) |
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **92 passed** (84 + 8), run three times with no flaky failures.

## Known limits / TODO
- **One todo case was wrong.** "Student 6 has no guardian" isn't true: the seed links student 6 to guardian 3. I dropped that case. A student with no guardian gets no alert because the query joins `StudentGuardians`; the opted-out guardian test checks the same "no recipient" outcome.
- **Two tests change seed rows for a moment:** `ReceivesAlerts` for student 4 / guardian 2, and user 12's `IsActive`. Each is restored in a `finally`. No other test class reads those values; I checked with grep.
- A teacher correcting Absent → Present after the alert is queued doesn't take it back (spec assumption 3).
- The 48 h window and the email wording are fixed in code, not in settings.
