# Developer docs — Milestone 13 Task 3: Web data layer for the Parent portal

## Summary
The MVC site can now call the two parent endpoints from Task 2 through the usual API-backed layering: typed `HttpClient` repository → service → view model. There are no pages yet (Task 4). The site still has no database access: the response records are copied into `Models/Api` because Web references no Core.

## Projects touched
WEB MVC.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `src/Klasly.Web/Models/Api/OverviewDtos.cs` | added | `StudentOverviewResponse`, `ClassOverview`, `AttendanceItem`, `GradeItem` (copies of the API's JSON shape). |
| `src/Klasly.Web/Repositories/Interfaces/IParentRepository.cs`, `Repositories/ParentRepository.cs` | added | `GET api/parent/children`, `GET api/parent/children/{id}/overview`. |
| `src/Klasly.Web/Services/Interfaces/IParentService.cs`, `Services/ParentService.cs` | added | Builds `ChildOverviewViewModel`. |
| `src/Klasly.Web/ViewModels/ChildOverviewViewModel.cs` | added | The overview + the sibling list for the switcher. |
| `src/Klasly.Web/Program.cs` | modified | `AddApiClient<IParentRepository, ParentRepository>()` (carries the JWT) and the service. |

## Key code
```csharp
public async Task<ChildOverviewViewModel> GetChildAsync(int studentId, CancellationToken ct)
{
    // Overview first: an unlinked id fails with the API's 403 before the second call.
    var overview = await parents.GetOverviewAsync(studentId, ct);
    return new ChildOverviewViewModel(overview, await parents.ListChildrenAsync(ct));
}
```
`LinkedStudentResponse` already existed in `Models/Api/AccountDtos.cs` (admin parent links) and is reused. API errors become `ApiException` in `GetApiAsync`, and the existing `ApiExceptionFilter` turns them into pages.

## Database changes
None.

## How it was verified
`dotnet build src/Klasly.Web` → Build succeeded, no warnings. No unit test: these are pass-through calls with no logic. The pages in Task 4 are checked end to end in Task 5.

## Known limits / TODO
Pages, redirect and nav come in Task 4.
