# How to use — Milestone 11 Task 1 (risk rules)

There is nothing new to click yet. This task adds the rule that decides whether a student is at-risk; the Worker starts using it in Task 4. You can check the rule by running its tests.

## Prerequisites
- .NET 10 SDK.
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) for the full test run. The risk-rule tests themselves don't need a database.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Run only the new tests:
   ```
   dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.RiskRulesTests
   ```
3. Expect `succeeded: 10`.
4. Run everything: `dotnet test --solution Klasly.slnx` → expect `succeeded: 43`.

## Example: what the rule decides
With the seeded settings `AbsenceThreshold = 0.20`, `PassingGrade = 75`:

| Absent / counted sessions | Weighted average | At-risk? | Why |
| --- | --- | --- | --- |
| 1 / 5 | none yet | **Yes** | 20% absence is exactly the threshold |
| 0 / 5 | 75.00 | No | exactly 75 passes |
| 0 / 5 | 74.99 | **Yes** | below 75 |
| 0 / 5 | 74.996 | No | shown as 75.00, so it passes |
| 0 / 0 | none yet | No | nothing to judge yet |

## Troubleshooting
- **`The name 'RiskRules' does not exist`**: you are on an older commit; pull the latest `dev`.
- **Other tests fail with a LocalDB error**: start LocalDB with `sqllocaldb start MSSQLLocalDB`. The risk-rule tests still pass without it.
- **`--filter-class` not recognised**: you are on an old SDK. `global.json` pins 10.0.x; run `dotnet --version` to check.
