# Implementation Plan: Milestone 11 — Worker: risk calculation + RiskFlags snapshots

> **Draft — waiting for team approval.** Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-10). Task details and acceptance criteria are in [`todo-m11.md`](./todo-m11.md). Milestone 10 stays in `plan-m10.md` / `todo-m10.md` (its Task 9, the Firebase setup, is still open and doesn't block this milestone).

## Context

Milestone 10 is committed (`8f31453`). Milestone 11 implements guide §27.9 #8: flag a student at-risk when their absence rate reaches 20% or their weighted average falls below 75. It runs as the third step of the Worker's `WorkerTickJob`.

**Decisions (spec open questions, 2026-11-10):**
- Q1: write a snapshot only when there is none yet or when AbsenceRate, WeightedAverage or IsAtRisk changed.
- Q2: the absence rate counts the student's own records in Finalized sessions.
- Q3: a blank grade is skipped for that student.
- Q4: a missing or unparsable setting fails the step and logs an error.

**Outcome:** after each tick, every Active enrollment in the active term has an up-to-date latest `RiskFlags` row. No schema change, no new package, no endpoint, no page.

## Reuse (don't rebuild)

- **Worker pattern:** `AttendanceFinalizer` (`src/Klasly.Worker/Services/`). Primary constructor, `now` passed in by the job, logger, scoped registration in `Program.cs`.
- **Settings:** `ISystemSettingRepository.GetValueAsync(key)`. It's already registered and its keys are seeded.
- **Entities:** `RiskFlag`, `Enrollment.RiskFlags`, and `RiskFlagConfiguration`, with the precision (5,4) / (5,2) and the latest-first index.
- **Tests:**
  - `DatabaseFixture` (LocalDB `Klasly_Tests`, real migrations);
  - `TestData.NewClassAsync(db, seats…)`, which creates a class in seeded term 1, the active term;
  - the own-rows-only checking style of `AttendanceFinalizerTests`.

## Design notes

- **`RiskRules.Compute`** (static, no database) returns `(Rate, Average, AtRisk)`. The code is in the spec's Code style section. The flag is decided on the rounded values.
- **One query** (`RiskRepository.ListInputsAsync`). It starts from `Enrollments` where `Status == Active && Class.Term.IsActive` and projects one `RiskInput` per enrollment with:
  - `AbsentCount`: the student's `Absent` records in this class's `Finalized` sessions;
  - `CountedCount`: the same records, minus `Excused`;
  - the student's grades in this class as a small list of `(Score, MaxScore, WeightPercent)`;
  - the latest snapshot: `RiskFlags` ordered by `ComputedAt DESC, RiskFlagId DESC`, first or null.
  
  EF Core turns this into one SQL statement with subqueries. **The weighted sums are added up in C# decimals**, not in SQL, because SQL Server's decimal division can round away digits and move a 74.995 across the boundary. `RiskInput` carries `WeightedSum` and `WeightSum` once summed, so `RiskRules` keeps the spec's signature.
- **`RiskCalculator.RecomputeAsync(nowUtc, ct)`:**
  1. Read and parse both settings (invariant culture). If either is missing or bad, throw `InvalidOperationException("Setting 'PassingGrade' is missing or not a number.")` before any query.
  2. List the inputs.
  3. Compute each row; add a `RiskFlag` only when it differs from the latest snapshot.
  4. One `SaveChangesAsync`.
  5. Return `(Enrollments, NewSnapshots, AtRisk)` for the job's log line.
- **Job:** `RecomputeAsync(now, ct)` comes after `MarkAbsentAndFinalizeAsync`, and the log line gains the three counts. The existing `[AutomaticRetry(Attempts = 0)]` covers a failed step: the next tick tries again.
- **Testing the bad-setting case without touching the shared test database:** `RiskCalculator` takes `ISystemSettingRepository`, so that one test passes a tiny stub that returns null. Deleting a seeded row would break tests running in parallel.

## Task list

### Phase 1: Rules and data
- [ ] Task 1: `RiskRules` + boundary unit tests (XS)
- [ ] Task 2: `IRiskRepository` / `RiskInput` / `RiskRepository` + query tests (M)

### Checkpoint A
- [ ] `dotnet build Klasly.slnx` clean, all tests pass, `has-pending-model-changes` reports none

### Phase 2: Worker step
- [ ] Task 3: `RiskCalculator` (settings, change-only snapshots, one save) + tests (S)
- [ ] Task 4: wire into `WorkerTickJob` + `Program.cs`; smoke run on the dev database (S)

### Checkpoint B: Complete
- [ ] Every spec success criterion met; reports + INDEX written; ready for review

## Risks and mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| EF Core can't translate the nested projection into one query (or splits it) | Med | Task 2 logs the SQL once (`ToQueryString()`) and checks it's a single statement. Fallback: two queries (counts, then grades for the same enrollments). Still a fixed number of queries, not one per enrollment. |
| Decimal rounding moves a value across 75.00 or 0.20 | High (wrong flag) | Sums in C# decimals; rounding with `AwayFromZero` to the column scale; boundary unit tests for 0.20, 75.00, 74.99 and 74.996. |
| Tests run in parallel and the step touches every enrollment | Low | Each test makes its own class and checks only its own enrollments' rows, the same as `AttendanceFinalizerTests`. |
| Two snapshots written in the same second (tests, or a manual re-run) | Low | "Latest" orders by `ComputedAt DESC, RiskFlagId DESC`. |

## Out of scope

- Queueing the "became at-risk" email (Milestone 12, which reads the change between the two newest snapshots).
- Any page or endpoint showing flags (Milestone 13).
- Cleaning up old snapshots.

## Open questions

None. Every spec question was answered.
