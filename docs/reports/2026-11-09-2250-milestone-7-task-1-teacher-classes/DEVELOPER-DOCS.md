# Milestone 7 · Task 1 — Teacher area + My classes page

## Summary
Asked: `/build` the next task of Milestone 7 (Task 1 in `tasks/todo.md`).
Done: a new `Teacher` area. A teacher who opens `/` lands on **My classes** (`/Teacher/Classes`), a table of their own classes from `GET api/teacher/classes`, active term first, in the order the API sends. The navbar now shows "My classes" for teachers. It is built from the same Controller → Service → API-backed Repository → typed `HttpClient` chain as the Admin area.

## Projects touched
WEB MVC only. No API, EF Core, database, Worker or Android changes. No new package.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Repositories/Interfaces/ISessionRepository.cs` | added | The teacher's API calls. Only `ListMyClassesAsync` for now; sessions, QR and roster come in Tasks 2–5 |
| `src/Klasly.Web/Repositories/SessionRepository.cs` | added | Calls `GET api/teacher/classes` with the user's JWT |
| `src/Klasly.Web/Services/Interfaces/ISessionService.cs` | added | Service contract for the Teacher pages |
| `src/Klasly.Web/Services/SessionService.cs` | added | Passes the class list through for now; it will shape the session pages later |
| `src/Klasly.Web/Areas/Teacher/Controllers/ClassesController.cs` | added | `[Area("Teacher"), Authorize(Roles = "Teacher")]`, `Index` = My classes |
| `src/Klasly.Web/Areas/Teacher/Views/_ViewImports.cshtml`, `_ViewStart.cshtml` | added | Copies of Admin's, so the area's views get the same usings, tag helpers and layout |
| `src/Klasly.Web/Areas/Teacher/Views/Classes/Index.cshtml` | added | The My classes table, with an empty state |
| `src/Klasly.Web/Controllers/HomeController.cs` | modified | Teacher → `/Teacher/Classes` (Admin → `/Admin` as before) |
| `src/Klasly.Web/Views/Shared/_Layout.cshtml` | modified | The nav list is picked by role: Admin's 4 links, or the teacher's "My classes" |
| `src/Klasly.Web/Program.cs` | modified | Registers `ISessionRepository` (typed client) and `ISessionService` |
| `tasks/todo.md` | modified | Task 1 ticked |

`Views/Home/Index.cshtml` was **not** changed, though the plan listed it. Students are already refused at login (Milestone 5) and teachers are now redirected, so the page only ever shows to Parents, and its text already reads "Your parent pages are coming soon."

## Key code
```csharp
// HomeController — one portal per role
public IActionResult Index() =>
    User.IsInRole("Admin") ? RedirectToAction("Index", "Home", new { area = "Admin" })
    : User.IsInRole("Teacher") ? RedirectToAction("Index", "Classes", new { area = "Teacher" })
    : View();

// SessionRepository — the API sorts: active term first, then subject code and section
public Task<List<ClassResponse>> ListMyClassesAsync(CancellationToken ct) =>
    http.GetApiAsync<List<ClassResponse>>("api/teacher/classes", ct);
```
```cshtml
@* _Layout — nav items by role *@
var navArea = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Teacher") ? "Teacher" : null;
var navItems = navArea switch
{
    "Admin" => new[] { ("Terms", "Terms"), ("Subjects", "Subjects"), ("Accounts", "Accounts"), ("Classes", "Classes") },
    "Teacher" => new[] { ("Classes", "My classes") },
    _ => []
};
```

## Database changes
None.

## How it was verified
- `dotnet build Klasly.slnx`: **0 errors, 0 warnings**.
- No test project exists (spec assumption 8), so there was no RED/GREEN test step. Checked by hand in the browser with the API and the website running on LocalDB:
  - `rosa.villanueva@klasly.test` → redirected to `/Teacher/Classes`. It shows one row, ELECT3 · BSCpE 3-A · 1st Sem 2026-27 · MWF 07:30-09:00 · CL-301 · 6 enrolled. The "My classes" nav link is marked `aria-current="page"`, and Sessions links to `/Teacher/Classes/Details/1`.
  - `admin@klasly.test` → `/Admin` with the same 4 nav links as before. `/Teacher/Classes` → Access denied.
  - `marites.mendoza@klasly.test` (Parent) → welcome page "Your parent pages are coming soon.", no nav links. `/Teacher/Classes` → Access denied.
- No package was added, so `dotnet list package` is unchanged from Milestone 6.

## Known limits / TODO
- The **Sessions** button links to `Classes/Details/{id}`, which is a 404 until Task 2.
- I did not log in as the second teacher (`mark.delossantos@klasly.test`). The API filters classes by teacher (checked in Milestone 6). Teacher B's session URL is checked in Task 3.
- The first login after a cold start can hit the website's 15 s API timeout while LocalDB wakes up ("Klasly can't reach the server"). Trying again works. This is not new to this task.
