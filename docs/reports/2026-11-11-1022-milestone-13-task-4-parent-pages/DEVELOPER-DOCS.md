# Developer docs — Milestone 13 Task 4: Parent area pages

## Summary
Parents now have a portal on the MVC site:
- **Logging in:** a parent lands on **My children**, or straight on the child's page when only one child is linked.
- **The child's page** has one card per class: attendance counts, absence rate, weighted average, an At risk / On track / Not computed yet badge with the early-warning line, and expandable attendance and grade tables. Times are in Manila.
- **Several children:** pills switch between them.
- **Another family's child:** opening one by URL shows Access denied with the API's reason.
- **Students** who log into the web see "Use the Klasly app on your phone".

## Projects touched
WEB MVC.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `src/Klasly.Web/Areas/Parent/Controllers/ChildrenController.cs` | added | Index (redirect when exactly one child) and Details. `[Area("Parent"), Authorize(Roles = "Parent")]`. |
| `src/Klasly.Web/Areas/Parent/Views/Children/Index.cshtml` | added | The children list, or the "No children are linked" message. |
| `src/Klasly.Web/Areas/Parent/Views/Children/Details.cshtml` | added | The child overview cards. |
| `src/Klasly.Web/Areas/Parent/Views/_ViewImports.cshtml`, `_ViewStart.cshtml` | added | Copied from the Teacher area. |
| `src/Klasly.Web/Controllers/HomeController.cs` | modified | Parent → `Parent/Children/Index`. |
| `src/Klasly.Web/Views/Shared/_Layout.cshtml` | modified | "My children" nav item for parents. |
| `src/Klasly.Web/Views/Home/Index.cshtml` | modified | Only students land here now: "Use the Klasly app on your phone…" |
| `src/Klasly.Web/Infrastructure/ApiExceptionFilter.cs` | modified | On an API 403, the API's message is put in `TempData["Error"]`, so `_Alerts` shows it on the Access denied page. |
| `src/Klasly.Web/Views/Account/AccessDenied.cshtml` | modified | "It belongs to another account…", instead of "another role's portal", which was wrong for a parent changing the URL. |

## Key code
```csharp
public async Task<IActionResult> Index(CancellationToken ct)
{
    var children = await parents.GetChildrenAsync(ct);
    return children.Count == 1 ? RedirectToAction(nameof(Details), new { id = children[0].StudentId }) : View(children);
}
```
- Badge: `IsAtRisk` true → `text-bg-danger` "At risk"; false → `text-bg-success` "On track"; null → `text-bg-secondary` "Not computed yet".
- The early-warning line and "Updated <ManilaTime.Format>" show only when a snapshot exists.
- Percent and number formatting use `CultureInfo.InvariantCulture`: `0.75` → `75%`, `18.50` → `18.5`.

## Database changes
None.

## How it was verified
- `dotnet build Klasly.slnx` → Build succeeded, no warnings.
- Browser check (API + Web running, seeded fictional parent Marites):
  - logging in → `/Parent/Children` lists Aira Mendoza and Bryan Lacsamana;
  - `/Parent/Children/Details/1` shows 3 classes (CPE 401, ELECT3, IT302) with counts, rate, average or "No grades yet", the At risk badge, and "Updated Nov 11, 12:52 AM";
  - `/Parent/Children/Details/3` (another family's child) → Access denied, with the alert "You can only view your own children".

## Known limits / TODO
- The rate and badge reflect the Worker's last run (accepted in spec Q3). In the dev data, ELECT3 shows 75% while the live counts give 71% because the Worker last ran at 12:52 AM.
- An API 403 without a body (a policy 403) shows the generic "The server answered with status 403." alert. MVC's own role checks stop parents before that can happen.
- The visual polish and the one-child redirect check are Task 5.
