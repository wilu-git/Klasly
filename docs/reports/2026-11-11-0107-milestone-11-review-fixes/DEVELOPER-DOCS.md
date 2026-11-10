# Milestone 11 review fixes — settings range check, troubleshooting tip

## Summary
The Milestone 11 review found no correctness bugs and two small fixes, both now made:
- **Range check:** `RiskCalculator` now rejects risk settings outside a sensible range, not only unreadable ones. Before, `AbsenceThreshold = 20` (typed as a percent) or `PassingGrade = 0.75` was accepted and silently flagged nobody.
- **Troubleshooting tip:** the wrong third tip in the Milestone 11 `HOW-TO-USE.md` was replaced with a real one, and the first tip shows the new error text.

The spec's assumption 7 was updated to match.

## Projects touched
CONSOLE WORKER (and the test project, docs).

## Files changed
| Path | Change | Why |
| --- | --- | --- |
| `src/Klasly.Worker/Services/RiskCalculator.cs` | modified | `ReadDecimalAsync` takes a range check and an "expected" text for the message. |
| `tests/Klasly.Tests/RiskCalculatorTests.cs` | modified | 4 new cases: `20`, `0`, `0.75`, `101`. |
| `docs/reports/2026-11-11-0055-milestone-11-worker-risk-flags/HOW-TO-USE.md` | modified | New error text; the wrong "ticks every 5 minutes" tip replaced with "why no new snapshot". |
| `SPEC.md` | modified | Assumption 7 notes the ranges. |
| `docs/reports/…` + `INDEX.md` | added / modified | These reports. |

## Key code
```csharp
var threshold = await ReadDecimalAsync("AbsenceThreshold", v => v > 0 && v <= 1, "a fraction above 0 and up to 1, e.g. 0.20", ct);
var passing   = await ReadDecimalAsync("PassingGrade",     v => v >= 1 && v <= 100, "a grade from 1 to 100, e.g. 75", ct);
// → InvalidOperationException("Setting 'AbsenceThreshold' is missing or invalid: it must be a fraction above 0 and up to 1, e.g. 0.20.")
```
- `0` is refused for the threshold too, because it would flag every student.
- The earlier Milestone 11 report quotes the old message ("is missing or not a number"). It is left as written, since it records that build.

## Database changes
None.

## How it was verified
- RED: the 4 new cases failed (no exception thrown); the old 4 passed.
- GREEN: `RiskCalculatorTests` 8 passed.
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test --solution Klasly.slnx` → **56 passed**.

## Known limits / TODO
- The review notes for later milestones still apply:
  - Milestone 13 dashboards must show only Active enrollments, because a dropped student's last flag stays as it was.
  - The latest-flag lookup reads past terms' flags too. That's fine at school scale.
- I didn't trigger a failed tick to watch Hangfire log it.
