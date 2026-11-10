# Tasks: Milestone 11 — Worker risk calculation + RiskFlags snapshots

> From [`plan-m11.md`](./plan-m11.md) and [`SPEC.md`](../SPEC.md). Every task ends with `dotnet build Klasly.slnx` + `dotnet test --solution Klasly.slnx`, and writes the three reports + an INDEX line (CLAUDE.md §5).

## Task 0: Housekeeping
- [x] SPEC.md marked Approved with the answers; `plan-m11.md` + this file written.

## Task 1: Risk rules (test first)

**What:** `src/Klasly.Worker/Services/RiskRules.cs`, a static `Compute(absent, counted, weightedSum, weightSum, threshold, passing)` → `(Rate, Average, AtRisk)`.

**Acceptance:**
- [x] 1 of 5 absent → rate 0.2000, at-risk. 0 of 5 → 0, not at-risk. 1 of 3 → 0.3333.
- [x] Rate 0.1999 → not at-risk (against a 0.20 threshold).
- [x] Average exactly 75.00 → not at-risk; 74.99 → at-risk; a raw 74.996 → 75.00 → not at-risk.
- [x] No grades (weightSum 0) → average null, and only the rate decides. No counted sessions → rate 0.

**Verify:** `dotnet test --solution Klasly.slnx --filter RiskRulesTests`

**Files:** `src/Klasly.Worker/Services/RiskRules.cs`, `tests/Klasly.Tests/RiskRulesTests.cs`. **Depends on:** none.

## Task 2: Risk inputs query (test first)

**What:**
- `RiskInput` record in `Klasly.Core/DTOs`;
- `IRiskRepository` (`ListInputsAsync`, `Add`, `SaveChangesAsync`) in `Klasly.Core/Interfaces`;
- `RiskRepository` in `Klasly.Infrastructure/Repositories`;
- registration in `DependencyInjection.cs`.

**Acceptance:**
- [x] One row per Active enrollment in an active-term class. A Dropped enrollment, and a class in a new inactive term, are absent from the list.
- [x] Counts use only this class's Finalized sessions. Excused is left out of both counts, Late counts as attended, and a record in a Closed (not yet Finalized) session doesn't count.
- [x] Sums use only the student's own grades in this class. A blank grade adds nothing. Two assessments (18/20 weight 30, 30/50 weight 20) → WeightedSum 3900, WeightSum 50 (average 78.00).
- [x] The latest snapshot is the newest by `ComputedAt`, then `RiskFlagId`; null when there is none.
- [x] `ToQueryString()` shows a single SQL statement (or the fallback in the plan's risks table, noted in the report).

**Verify:** `dotnet test --solution Klasly.slnx --filter RiskRepositoryTests`; `has-pending-model-changes` reports none.

**Files:** `src/Klasly.Core/DTOs/RiskInput.cs`, `src/Klasly.Core/Interfaces/IRiskRepository.cs`, `src/Klasly.Infrastructure/Repositories/RiskRepository.cs`, `src/Klasly.Infrastructure/DependencyInjection.cs`, `tests/Klasly.Tests/RiskRepositoryTests.cs`. **Depends on:** none.

## Checkpoint A
- [x] Build clean, all tests pass (33 + the new ones), no pending model changes.

## Task 3: RiskCalculator (test first)

**What:** `src/Klasly.Worker/Services/RiskCalculator.cs`, with `RecomputeAsync(nowUtc, ct)` → `(Enrollments, NewSnapshots, AtRisk)`.

**Acceptance:**
- [x] First run → one snapshot per own enrollment, `ComputedAt = now`, values from `RiskRules`.
- [x] Second run with nothing changed → no new rows. A new Absent in a Finalized session, or a new grade → exactly one new row for that enrollment. Old rows are never changed.
- [x] Missing or unparsable `PassingGrade` / `AbsenceThreshold` (stub settings repository) → throws with the key in the message, and nothing is saved.
- [x] One `SaveChangesAsync` per run.

**Verify:** `dotnet test --solution Klasly.slnx --filter RiskCalculatorTests`

**Files:** `src/Klasly.Worker/Services/RiskCalculator.cs`, `tests/Klasly.Tests/RiskCalculatorTests.cs`. **Depends on:** Tasks 1, 2.

## Task 4: Wire into the tick + smoke run

**What:**
- `WorkerTickJob` calls `RecomputeAsync(now, ct)` after finalizing and logs `risk: X enrollments, Y new snapshots, Z at-risk`;
- `Program.cs` registers `AddScoped<RiskCalculator>()`;
- the placeholder comment is updated (Milestone 12 only).

**Acceptance:**
- [ ] Build and all tests pass.
- [ ] Smoke run on the dev `Klasly` database (`dotnet run --project src/Klasly.Worker`):
  - one tick writes a snapshot for each seeded active enrollment;
  - a second tick writes none;
  - after a seeded student gets 1 Absent of 5 Finalized sessions (or a grade below 75), the next tick writes one at-risk row;
  - checked with `SELECT TOP 20 * FROM RiskFlags ORDER BY ComputedAt DESC`.

**Files:** `src/Klasly.Worker/Jobs/WorkerTickJob.cs`, `src/Klasly.Worker/Program.cs`. **Depends on:** Task 3.

## Checkpoint B: Complete
- [ ] Every success criterion in SPEC.md met.
- [ ] `dotnet list Klasly.slnx package`: Microsoft.* still 10.x, nothing new. No migration.
- [ ] Reports + INDEX line written; ready for `/review`.
