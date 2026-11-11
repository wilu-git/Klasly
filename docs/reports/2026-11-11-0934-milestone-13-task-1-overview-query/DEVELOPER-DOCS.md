# Developer docs — Milestone 13 Task 1: student overview query

## Summary
Task 1 of Milestone 13 (dashboards) adds the read model that the student app and the parent portal will both use. `OverviewRepository.GetAsync(studentId)` returns one student's Active enrollments in the active term. Each class has subject, section, teacher, live attendance counts and records (newest first), every assessment with this student's score (null = not graded yet), and the latest `RiskFlags` snapshot (all null before the Worker's first run). It was written test first: 6 new LocalDB tests. There is no endpoint yet; that is Task 2.

## Projects touched
EF CORE (`Klasly.Core`, `Klasly.Infrastructure`), tests. No WEB API, WEB MVC, Worker or Android changes.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `src/Klasly.Core/DTOs/StudentOverviewResponse.cs` | added | `StudentOverviewResponse`, `ClassOverview`, `AttendanceItem`, `GradeItem`: the shared response for both roles (spec Q1). |
| `src/Klasly.Core/Interfaces/IOverviewRepository.cs` | added | `GetAsync(studentId, ct)` → overview or null. |
| `src/Klasly.Infrastructure/Repositories/OverviewRepository.cs` | added | The query. |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | modified | Registers `IOverviewRepository`. |
| `tests/Klasly.Tests/OverviewTests.cs` | added | 6 tests on the real schema. |
| `tasks/todo-m13.md`, `tasks/plan-m13.md` | modified | Task 1 ticked. |

## Key code
The shape comes from one EF projection with three nested parts, run as a **split query** (records × assessments in a single join would multiply rows). The 3-query fallback from the plan wasn't needed.

```csharp
.Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active && e.Class.Term.IsActive)
...
Records = context.AttendanceRecords
    .Where(a => a.StudentId == studentId && a.Session.ClassId == e.ClassId)
    .OrderByDescending(a => a.Session.StartTime).ThenByDescending(a => a.SessionId) ...
Grades = e.Class.Assessments.OrderBy(a => a.DatePosted).ThenBy(a => a.Title)
    .Select(a => new { ..., Mine = a.Grades.Where(g => g.StudentId == studentId)
                                           .Select(g => new { g.Score, g.PostedAt }).FirstOrDefault() })
Latest = e.RiskFlags.OrderByDescending(r => r.ComputedAt).ThenByDescending(r => r.RiskFlagId) ... .FirstOrDefault()
```

- The four counts are computed in C# from the same records list, so the counts and the list always agree.
- Status enums become strings in memory.
- Two small queries run first: the student header (null → the method returns null) and the active term's name (null when no term is active).
- The latest snapshot uses the same "newest time, then highest id" rule as `RiskRepository`.

## Database changes
None. `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration."

## How it was verified
- The tests were written first. Before the code existed, `dotnet build tests/Klasly.Tests` failed with CS0246 (`StudentOverviewResponse` / `ClassOverview` not found).
- `dotnet build Klasly.slnx` → Build succeeded, no warnings.
- `dotnet test --solution Klasly.slnx` → 115 passed, 0 failed.
- `--filter-class Klasly.Tests.OverviewTests` → 6 passed.
- `dotnet list Klasly.slnx package` → every Microsoft.* runtime package is 10.0.14. Nothing new was added.

## Known limits / TODO
- No endpoint and no ownership check yet. That is Task 2 (`OverviewService`, the student and parent controllers, `IsAtRisk = null` for students).
- "No active term → empty list" isn't tested directly: deactivating term 1 would break tests running in parallel. The inactive-term-class test covers the same filter.
- The tests share the "Risk" xUnit collection, so `RiskCalculatorTests` can't add a newer snapshot during the latest-risk test.
