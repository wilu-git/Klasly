# How to use — Milestone 13 Task 1 (student overview query)

This task adds the database query behind the coming dashboards. Nothing changes on any screen yet. Task 2 adds the API endpoints, Tasks 3–5 the parent web pages, and Tasks 6–9 the Android screens.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database (`dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`).
- .NET 10 SDK.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Build: `dotnet build Klasly.slnx`.
3. Run the new tests:
   ```
   dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.OverviewTests
   ```
   You should see `total: 6, succeeded: 6`.

## Example: what the overview contains
For one student, for each class they are actively enrolled in this term:

| Part | Example | Notes |
| --- | --- | --- |
| Class | ELECT3 — Mobile Development, BSCpE 3-A, the teacher's name | Dropped classes and classes from past terms are hidden. |
| Attendance counts | Present 8, Late 1, Absent 2, Excused 1 | Always up to date. |
| Attendance list | Nov 10, 9:00 AM — Absent (newest first) | One line per recorded session. |
| Grades | Quiz 1 — 18.5 / 20 (40%); Exam — not graded yet (60%) | Every assessment of the class appears, graded or not. |
| Risk | absence rate 30%, no average yet, at-risk, updated 9:05 AM | From the Worker's latest check; empty until the Worker has run. |

When a parent or student opens a dashboard (later tasks), this is the data they will see.

## Troubleshooting
- **Tests fail with "Cannot open database Klasly"**: start LocalDB (`sqllocaldb start MSSQLLocalDB`) and run the `dotnet ef database update` command above.
- **The risk fields are empty for real data**: the Worker hasn't run yet. Start it with `dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"`.
- **A build error about `IOverviewRepository`**: pull the latest code. The interface lives in `Klasly.Core/Interfaces`, and `Klasly.Infrastructure/DependencyInjection.cs` registers it.
