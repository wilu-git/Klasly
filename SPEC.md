# Spec: Milestone 11 — Worker: risk calculation + RiskFlags snapshots

> Status: **Approved** (2026-11-10). Open questions 1–4 answered as recommended: snapshot only on change, count the student's own records, skip blank grades, fail on a bad setting. Plan: `tasks/plan-m11.md` + `tasks/todo-m11.md`.
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.8 "Risk calculation", §27.6 RiskFlags / SystemSettings, §27.7 conventions, §27.9 #8, §27.10 boundary tests, the §"one query" note for the risk job) → `CLAUDE.md` (§3.5 Console Worker, §6 Milestone 11) → this spec.
> Milestone 10's spec is in git history (commit `8f31453`) and in `docs/reports/2026-11-10-2221-milestone-10-assessments-grades-push/`.

## What exists today

- **The table is ready.** `RiskFlags` (EnrollmentId, AbsenceRate `decimal(5,4)` 0–1, WeightedAverage `decimal(5,2)` null or 0–100, IsAtRisk, ComputedAt UTC default) with the `(EnrollmentId, ComputedAt DESC)` index. `RiskFlag` entity and `Enrollment.RiskFlags` exist. Nothing writes or reads it yet.
- **Settings are seeded:** `AbsenceThreshold = 0.20`, `PassingGrade = 75`. `ISystemSettingRepository.GetValueAsync(key)` reads them.
- **The Worker:** `WorkerTickJob` runs `AttendanceFinalizer` (close ended sessions → mark Absent + Finalize) and has a placeholder comment for Milestone 11. `[DisableConcurrentExecution]` and `[AutomaticRetry(Attempts = 0)]` are in place.
- **Inputs exist:** Finalized sessions with Present / Late / Absent / Excused records (Milestone 9); assessments with weights and grades, where a blank score means "not graded yet" (Milestone 10).
- **Tests:** `tests/Klasly.Tests` (xUnit, LocalDB `Klasly_Tests` built by the real migrations), 33 passing.

## Assumptions (correct these before we build)

1. **Who gets a snapshot:** every **Active** enrollment in a class of the **active term**. Dropped enrollments and past terms are left alone; their last snapshot stays as it was.
2. **Absence Rate** for one enrollment = the student's **Absent** records ÷ the student's **Present + Late + Absent** records, counting only **Finalized** sessions of that class. Excused is left out of both counts; Cancelled sessions have no records, so they drop out by themselves. Counting the student's own records (not all class sessions) means a student who enrolled late is not charged for sessions held before they joined.
3. **No finalized sessions yet → Absence Rate = 0** (the column is NOT NULL and 0 can't trigger a flag).
4. **Weighted Average** = Σ(score ÷ max × 100 × weight) ÷ Σ(weight), over the assessments **this student has a grade for**. An assessment the student has no grade on (blank score) is skipped for that student. **NULL** when the student has no grades.
5. **Rounding:** Absence Rate to 4 decimals, Weighted Average to 2 decimals (the column sizes), `MidpointRounding.AwayFromZero`. **IsAtRisk is decided on the rounded values**, so what a dashboard shows always matches the flag (74.996 is shown as 75.00 and is not at-risk).
6. **At-risk** = Absence Rate ≥ `AbsenceThreshold` **OR** (Weighted Average is not NULL **AND** Weighted Average < `PassingGrade`). Exactly 0.20 → at-risk; exactly 75 → not.
7. **Settings are read once per tick** with invariant-culture parsing. A missing or unparsable setting **fails the step** with a logged error (no silent default); the next tick tries again.
8. **One query, one save.** The repository loads every enrollment's inputs in one query (the guide asks for this because it runs every 5 minutes) plus the latest snapshot per enrollment; the rules run in C#; all new rows go in one `SaveChanges`. All snapshots of one tick share the tick's `now`.
9. **Order in the tick:** after `MarkAbsentAndFinalizeAsync`, so newly finalized Absents count in the same tick.
10. **No alerts in this milestone.** Queueing the "became at-risk" email is Milestone 12. This milestone only makes sure the snapshot history lets Milestone 12 find the change (see Q1).
11. **No schema change, no new package, no API endpoint, no page.** Dashboards that read the flags are Milestone 13.

## Objective

Each Worker tick turns attendance and grades into an early-warning **at-risk flag** per student per class, so parents (Milestones 12–13) and teachers can act before the term ends. The flag is an indicator for review, not a final judgment (§27.5).

### User stories

- As a **parent**, I want my child flagged when they miss 20% of classes or their average drops below 75, so the alert in Milestone 12 reaches me early.
- As a **teacher**, I want the flag recomputed automatically after every session and every grade change, without pressing anything.
- As the **team**, we want the boundary cases (exactly 20%, exactly 75) proven by tests, because §27.10 lists them for hardening.

## Design

```
WorkerTickJob.RunAsync
  ├─ AttendanceFinalizer.CloseEndedSessionsAsync     (Milestone 9)
  ├─ AttendanceFinalizer.MarkAbsentAndFinalizeAsync  (Milestone 9)
  └─ RiskCalculator.RecomputeAsync(now)              ← new
        1. read AbsenceThreshold + PassingGrade
        2. IRiskRepository.ListInputsAsync()  → one row per active enrollment:
             EnrollmentId, AbsentCount, CountedCount, WeightedSum, WeightSum,
             latest snapshot (AbsenceRate, WeightedAverage, IsAtRisk) or null
        3. RiskRules.Compute(...) per row  → (rate, average, isAtRisk)
        4. add RiskFlag rows (all, or only changed ones — Q1)
        5. one SaveChanges; log "risk: X enrollments, Y new snapshots, Z at-risk"
```

`RiskRules.Compute` is a small static method with no database, so the boundary tests are plain unit tests.

## Commands

```
Build:    dotnet build Klasly.slnx
Test:     dotnet test Klasly.slnx
Worker:   dotnet run --project src/Klasly.Worker
Model:    dotnet ef migrations has-pending-model-changes -p src/Klasly.Infrastructure -s src/Klasly.API   (must say none)
Packages: dotnet list Klasly.slnx package   (Microsoft.* still 10.x, nothing new)
Check:    SELECT TOP 20 * FROM RiskFlags ORDER BY ComputedAt DESC;
```

## Project structure (files this milestone touches)

| Path | New / changed | Why |
| --- | --- | --- |
| `src/Klasly.Core/Interfaces/IRiskRepository.cs` | new | Reads the per-enrollment inputs + latest snapshot; adds snapshots. |
| `src/Klasly.Core/DTOs/RiskInput.cs` | new | The one-row-per-enrollment record the query returns. |
| `src/Klasly.Infrastructure/Repositories/RiskRepository.cs` | new | The single EF Core query (grouped sums) and the save. |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | changed | Register `IRiskRepository`. |
| `src/Klasly.Worker/Services/RiskRules.cs` | new | Pure rule: rates, rounding, at-risk. |
| `src/Klasly.Worker/Services/RiskCalculator.cs` | new | Reads settings, runs the rule, writes snapshots. |
| `src/Klasly.Worker/Jobs/WorkerTickJob.cs` | changed | Call `RecomputeAsync` as the third step; extend the log line. |
| `src/Klasly.Worker/Program.cs` | changed | `AddScoped<RiskCalculator>()`. |
| `tests/Klasly.Tests/RiskRulesTests.cs` | new | Boundary unit tests. |
| `tests/Klasly.Tests/RiskCalculatorTests.cs` | new | LocalDB tests of the query + snapshots. |

## Code style

Same as Milestone 9's `AttendanceFinalizer`: primary constructors, `CancellationToken` all the way, the job passes `now` in, decimals (never `double`) for scores and rates.

```csharp
public static class RiskRules
{
    public static (decimal Rate, decimal? Average, bool AtRisk) Compute(
        int absent, int counted, decimal weightedSum, decimal weightSum, decimal threshold, decimal passing)
    {
        var rate = counted == 0 ? 0m : Math.Round((decimal)absent / counted, 4, MidpointRounding.AwayFromZero);
        decimal? average = weightSum == 0 ? null : Math.Round(weightedSum / weightSum, 2, MidpointRounding.AwayFromZero);
        return (rate, average, rate >= threshold || average < passing); // null < passing is false
    }
}
```

## Testing strategy

- **Unit (`RiskRulesTests`)**, no database: 1 of 5 absent (0.20) → at-risk; 0 of 5 and 0.1999 → not; average exactly 75.00 → not; 74.99 → at-risk; 74.996 → rounds to 75.00 → not; no grades → average NULL and not at-risk on grades; no counted sessions → rate 0; 1 of 3 → 0.3333.
- **Database (`RiskCalculatorTests`)**, LocalDB fixture like `AttendanceFinalizerTests`:
  - Excused leaves both counts; Late counts as attended; records in a Closed (not yet Finalized) session don't count.
  - A blank grade is skipped; weights of ungraded assessments don't count; the weighted formula matches a hand calculation.
  - Dropped enrollment and an inactive-term class get no snapshot.
  - Snapshot behaviour per Q1 (unchanged → no row / always a row; changed → a new row).
  - Missing `PassingGrade` → the step throws and nothing is saved.
- **Smoke (manual):** run the Worker against the dev database, finalize a session where a seeded student is absent 1 of 5, post a grade below 75 for another, wait one tick, check `RiskFlags`.

## Boundaries

- **Always:** keep the rules in `RiskRules` / `RiskCalculator`, not in the job; use `now` from the job; decimals; run build + tests; write the three reports after each code prompt.
- **Ask first:** any schema change or new index; changing the guide's formulas; adding an API endpoint or page for the flags (Milestone 13); deleting old snapshots.
- **Never:**
  - queue emails or push here (Milestone 12);
  - update or delete existing `RiskFlags` rows (they are history);
  - call the risk step from the Web API;
  - hard-code the 0.20 / 75 thresholds.

## Success criteria

- [ ] After a tick, every Active enrollment in the active term has a latest `RiskFlags` row whose AbsenceRate, WeightedAverage and IsAtRisk follow assumptions 2–6.
- [ ] Exactly 20% absence → at-risk; exactly 75 average → not at-risk (tests).
- [ ] Excused, Cancelled, not-yet-finalized sessions and blank grades are excluded as described (tests).
- [ ] Changing `AbsenceThreshold` / `PassingGrade` in `SystemSettings` changes the next tick's flags with no code change.
- [ ] The step runs as the third step of `WorkerTickJob`, after finalizing; one input query and one save per tick.
- [ ] No migration (`has-pending-model-changes` reports none); no new package; Microsoft.* still 10.x; all tests pass.
- [ ] Reports written; INDEX line added.

## Open questions (my recommendation first)

1. **Snapshot every tick, or only on change?** The guide says "each run writes a snapshot per enrollment". Every 5 minutes that is 288 rows per enrollment per day (about 290,000 rows a day for 1,000 enrollments), almost all identical.
   **Recommend: write a row only when there is no snapshot yet or when AbsenceRate, WeightedAverage or IsAtRisk differs from the latest one.** Dashboards still read the latest row, the trend history keeps every real change, and Milestone 12 finds "became at-risk" by checking that the latest row is at-risk while the one before it is not (or there is none). ComputedAt then means "when this value was first computed". Alternative: follow the guide literally and write every tick.
2. **Late enrolment / denominator:** count the student's own records (assumption 2, recommended) or every Finalized session of the class? The second charges a late-enrolled student nothing (no record → not Absent) but also lowers their rate unfairly. **Recommend the student's own records.**
3. **Missing grade on a graded assessment:** skip it for that student (assumption 4, recommended, since the guide says "ungraded assessments are excluded" and the grade sheet allows blanks) or count it as 0? **Recommend skip.**
4. **Bad or missing setting:** fail the step and log (recommended, settings are seeded and shouldn't be guessed), or fall back to 0.20 / 75? **Recommend fail.**
