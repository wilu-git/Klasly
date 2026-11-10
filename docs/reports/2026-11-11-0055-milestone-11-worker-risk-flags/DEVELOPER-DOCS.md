# Milestone 11 Tasks 2–4 — Worker risk flags (milestone complete)

## Summary
`/build` continued `tasks/todo-m11.md` after Task 1 (`RiskRules`, report `2026-11-11-0003-…`).
- **Task 2:** a one-command query that loads each active enrollment's attendance counts, grades and latest snapshot.
- **Task 3:** `RiskCalculator`, which reads the thresholds and writes a `RiskFlags` row only when the values change.
- **Task 4:** the risk step is wired into `WorkerTickJob` after finalizing, and checked with a live Worker run on the dev database.

Every spec success criterion is met. No schema change and no new package.

## Projects touched
CONSOLE WORKER, EF CORE (repository only), MSSQL (data written by the smoke run; no schema change).

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Core/DTOs/RiskInput.cs` | added | `RiskInput` (one row per enrollment) and `RiskSnapshot` (latest stored values). |
| `src/Klasly.Core/Interfaces/IRiskRepository.cs` | added | `ListInputsAsync`, `Add`, `SaveChangesAsync`. |
| `src/Klasly.Infrastructure/Repositories/RiskRepository.cs` | added | The single EF Core query; weighted sums in C#. |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | modified | Registers `IRiskRepository`. |
| `src/Klasly.Worker/Services/RiskCalculator.cs` | added | Settings → rules → change-only snapshots → one save. |
| `src/Klasly.Worker/Jobs/WorkerTickJob.cs` | modified | Third step + log line. |
| `src/Klasly.Worker/Program.cs` | modified | `AddScoped<RiskCalculator>()`. |
| `tests/Klasly.Tests/DatabaseFixture.cs` | modified | `CreateContext(params IInterceptor[])` so a test can count SQL commands. |
| `tests/Klasly.Tests/RiskRepositoryTests.cs` | added | 5 query tests (counts, sums, filters, latest, one command). |
| `tests/Klasly.Tests/RiskCalculatorTests.cs` | added | 4 tests (first run, change-only, bad settings ×2). |
| `SPEC.md`, `tasks/plan-m11.md`, `tasks/todo-m11.md` | modified | Criteria and tasks ticked. |

## Key code
The query, one SQL command (checked by a test with a `DbCommandInterceptor`):
```csharp
context.Enrollments.AsNoTracking()
    .Where(e => e.Status == EnrollmentStatus.Active && e.Class.Term.IsActive)
    .Select(e => new {
        e.EnrollmentId,
        Absent  = context.AttendanceRecords.Count(a => a.StudentId == e.StudentId && a.Session.ClassId == e.ClassId
                    && a.Session.Status == SessionStatus.Finalized && a.Status == AttendanceStatus.Absent),
        Counted = /* same, Status != Excused */,
        Grades  = context.Grades.Where(g => g.StudentId == e.StudentId && g.Assessment.ClassId == e.ClassId)
                    .Select(g => new { g.Score, g.Assessment.MaxScore, g.Assessment.WeightPercent }).ToList(),
        Latest  = e.RiskFlags.OrderByDescending(r => r.ComputedAt).ThenByDescending(r => r.RiskFlagId)
                    .Select(r => new RiskSnapshot(r.AbsenceRate, r.WeightedAverage, r.IsAtRisk)).FirstOrDefault()
    })
// then in C#: WeightedSum = Σ(Score × 100 × Weight ÷ Max), WeightSum = Σ Weight
```
Change-only snapshots (spec Q1):
```csharp
var (rate, average, isAtRisk) = RiskRules.Compute(...);
if (input.Latest == new RiskSnapshot(rate, average, isAtRisk)) continue; // unchanged: no new history row
risks.Add(new RiskFlag { ..., ComputedAt = nowUtc });
```
Settings are parsed with `CultureInfo.InvariantCulture`. A missing or bad value throws `InvalidOperationException("Setting 'PassingGrade' is missing or not a number.")` before any read or write. Hangfire logs the failed tick (`AutomaticRetry(Attempts = 0)`) and the next tick retries.

## Database changes
None to the schema; `has-pending-model-changes` reports no changes. The smoke run wrote 10 `RiskFlags` rows to the dev `Klasly` database. The Worker's existing Milestone 9 step also closed and finalized one ended session during that run.

## How it was verified
- **Task 2:** I wrote `RiskRepository` before its tests (not test-first). To make up for it, I broke the query on purpose three ways: dropped the active-term filter, counted Excused, and reversed the id tie-break. The tests failed each time; then I restored the file. That showed the Excused test was too weak (one Excused and one Late gave the same total), so it now uses two Excused records.
- **Task 3:** test-first. The build failed with `CS0246 RiskCalculator not found`, then 4 tests passed.
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **52 passed** (43 + 9).
- `dotnet ef migrations has-pending-model-changes …` → "No changes have been made to the model since the last migration."
- `dotnet list Klasly.slnx package` → every Microsoft.* package at 10.0.14 (the test SDK has its own 18.x numbering); no package added.
- **Smoke run:** `dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"` for 2.5 minutes:
  - tick 1: `risk for 10 enrollments, 10 new snapshots, 10 at-risk`;
  - ticks 2 and 3: `0 new snapshots`.
  
  Values hand-checked against SQL counts: 12/16 → 0.7500, 16/17 → 0.9412, 15/17 → 0.8824, and grades 18/20 → 90.00, 13/20 → 65.00. All 10 are at-risk because most dev sessions were finalized with nobody scanning.

## Known limits / TODO
- I didn't test the "becomes at-risk" change on the dev database by hand, because every dev enrollment was already at-risk. `RiskCalculatorTests.Unchanged_values_write_nothing_and_a_change_writes_one_new_row` covers it.
- The risk test classes share the xUnit collection "Risk" (not run in parallel with each other), because the calculator writes snapshots for every enrollment in the test database.
- Next: Milestone 12 queues the "became at-risk" email. It should treat "latest row at-risk, previous row not at-risk or missing" as the trigger, and key the alert on that `RiskFlagId`.
