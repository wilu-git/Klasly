# Developer docs — Milestone 13 Task 2: overview service + student and parent endpoints

## Summary
Task 2 of Milestone 13 exposes Task 1's overview through the Web API, with the ownership rules in a new `OverviewService`. Three endpoints:
- a student reads their own overview, with the at-risk flag left out (spec Q2);
- a parent lists their linked children (reusing `AccountService.ListLinkedStudentsAsync`, which the admin page already uses);
- a parent reads a linked child's overview. Any other id, existing or not, gets 403 "You can only view your own children" (spec Q4).

Tests were written first (5 new). The Checkpoint A smoke test against the running API passed.

## Projects touched
WEB API, tests.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `src/Klasly.API/Services/Interfaces/IOverviewService.cs` | added | `GetForStudentAsync`, `GetForParentAsync`. |
| `src/Klasly.API/Services/OverviewService.cs` | added | Token user → student/guardian, link check, at-risk removal for students. |
| `src/Klasly.API/Controllers/ParentController.cs` | added | `GET api/parent/children`, `GET api/parent/children/{studentId}/overview` (`ParentOnly`). |
| `src/Klasly.API/Controllers/StudentsController.cs` | modified | `GET api/students/me/overview`. |
| `src/Klasly.API/Program.cs` | modified | Registers `IOverviewService`. |
| `tests/Klasly.Tests/OverviewTests.cs` | modified | 5 service tests (4 facts + a 2-case theory). |
| `tasks/plan-m13.md`, `tasks/todo-m13.md` | modified | Task 2 and Checkpoint A ticked. |

## Key code
```csharp
public async Task<StudentOverviewResponse> GetForParentAsync(int parentUserId, int studentId, CancellationToken ct)
{
    var guardian = (await users.GetByIdAsync(parentUserId, ct))?.Guardian;
    // Unknown ids get the same 403 as another family's child, so ids can't be probed.
    if (guardian is null || await users.GetLinkAsync(studentId, guardian.GuardianId, ct) is null)
        throw new ForbiddenException("You can only view your own children");
    return await GetAsync(studentId, ct);
}
```
- Students: `overview with { Classes = … c with { IsAtRisk = null } }`. The absence rate and weighted average stay.
- The ids come only from the JWT `sub`. The student route has no id at all.
- The children list reuses the existing `IAccountService.ListLinkedStudentsAsync(parentUserId)` and `LinkedStudentResponse`, so no new code or DTO was needed.

## Database changes
None.

## How it was verified
- The tests were written first. Before the service existed, the build failed with CS0246 (`OverviewService` not found).
- `dotnet build Klasly.slnx` → Build succeeded, no warnings.
- `dotnet test --solution Klasly.slnx` → 120 passed, 0 failed.
- **Checkpoint A** (API running at `https://localhost:7256`, seeded fictional accounts):

| Call | Result |
| --- | --- |
| Student Aira → `GET students/me/overview` | 200, 3 classes, `isAtRisk` null in every class |
| Parent Marites → `GET parent/children` | 200, `[1 Aira Mendoza, 2 Bryan Lacsamana]` |
| Parent Marites → `GET parent/children/1/overview` | 200, counts and risk present (`isAtRisk: true` on a class with 100% absence) |
| Parent Marites → `GET parent/children/3/overview` | 403 problem-details, detail "You can only view your own children" |
| Teacher Rosa → either route | 403 |
| No token | 401 |

## Known limits / TODO
- Full role-access tests (every route × every role) are Milestone 14, per the spec.
- Plan correction: every seeded parent has two children (guardian 1 → students 1, 2; guardian 2 → 3, 4; guardian 3 → 5, 6). Task 5's "one child → redirect" check now unlinks one child in Admin › Accounts for the test and links it back afterwards (`todo-m13.md` updated).
