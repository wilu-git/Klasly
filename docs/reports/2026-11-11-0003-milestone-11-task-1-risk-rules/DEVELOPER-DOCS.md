# Milestone 11 Task 1 — Risk rules

## Summary
`/build` picked the next pending task in `tasks/todo-m11.md`: the at-risk rule on its own. `RiskRules.Compute` turns absence counts and weighted-grade sums into a rounded Absence Rate, a rounded Weighted Average (or null) and the IsAtRisk flag (guide §27.8). The tests were written first and failed (compile error, no `RiskRules` yet), then passed. Nothing calls the rule yet; Tasks 2–4 add the query, the calculator and the Worker step. The approved spec and plan were committed first as `35b772d`.

## Projects touched
CONSOLE WORKER (and the test project).

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Worker/Services/RiskRules.cs` | added | The pure at-risk rule: rates, rounding to the column scales, the flag. |
| `tests/Klasly.Tests/RiskRulesTests.cs` | added | 10 boundary tests (exactly 20%, exactly 75, 74.99, 74.996, no grades, no sessions). |
| `tasks/todo-m11.md` | modified | Task 1 ticked. |
| `docs/reports/…` + `INDEX.md` | added / modified | These reports. |

## Key code
```csharp
public static (decimal Rate, decimal? Average, bool AtRisk) Compute(
    int absent, int counted, decimal weightedSum, decimal weightSum, decimal threshold, decimal passing)
{
    var rate = counted == 0 ? 0m : Math.Round((decimal)absent / counted, 4, MidpointRounding.AwayFromZero);
    decimal? average = weightSum == 0 ? null : Math.Round(weightedSum / weightSum, 2, MidpointRounding.AwayFromZero);
    return (rate, average, rate >= threshold || average < passing); // null < passing is false
}
```
- Rate uses 4 decimals and Average uses 2, matching `RiskFlags.AbsenceRate decimal(5,4)` and `WeightedAverage decimal(5,2)`.
- The flag is decided on the rounded values (spec assumption 5), so 74.996 → 75.00 → not at-risk.
- `decimal` everywhere, never `double`, so 1/5 is exactly 0.2000.

## Database changes
None.

## How it was verified
- RED: `dotnet build tests/Klasly.Tests` → CS0103 "The name 'RiskRules' does not exist".
- GREEN: `dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.RiskRulesTests` → 10 passed.
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → 43 passed (33 before + 10 new).

## Known limits / TODO
- Not wired to anything yet. Next: Task 2 (risk inputs query), Task 3 (`RiskCalculator`), Task 4 (the `WorkerTickJob` step).
