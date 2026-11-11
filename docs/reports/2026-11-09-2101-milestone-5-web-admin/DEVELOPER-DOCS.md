# Milestone 5 — Web MVC Admin portal (Tasks 2–9)

## Summary
Asked: continue Milestone 5 from Task 2 through Task 9 (Task 1, login/logout, is in `docs/reports/2026-11-09-2034-milestone-5-task-1-login/`).
Done: the website now has the shared API plumbing (bearer token on every call, one exception filter for 401/403/404/API down, API errors shown on forms) and the full Admin portal: Terms, Subjects, Accounts with parent–student links, Classes with enrollments. Every Milestone 4 admin endpoint is reachable from a page or button. `/impeccable` pass done; the full 12-step smoke test passes over HTTPS.

## Projects touched
WEB MVC only. No API, EF Core, database, Worker or Android changes. No new packages.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Program.cs` | modified | Exception filter, `AddApiClient<,>` helper (typed client + 15 s timeout + bearer handler), services, Area route, `UseStatusCodePages`, "session ended" on cookie expiry |
| `src/Klasly.Web/Infrastructure/BearerTokenHandler.cs` | added | Adds the JWT stored in the auth cookie to every API call |
| `src/Klasly.Web/Infrastructure/ApiExceptionFilter.cs` | added | 401 → sign out + login with message, 403 → AccessDenied, 404 → NotFound, API unreachable → `ApiUnavailable` page (503, logged) |
| `src/Klasly.Web/Infrastructure/ModelStateExtensions.cs` | added | `AddApiErrors(ex, model)`: API field errors next to inputs, other messages in the summary |
| `src/Klasly.Web/Infrastructure/ApiResponse.cs` | modified | `GetApiAsync/PostApiAsync/PutApiAsync/DeleteApiAsync` extensions; message falls back to the first field error |
| `src/Klasly.Web/Models/Api/{Term,Subject,Account,Class}Dtos.cs` | added | Copies of the API's JSON shapes (enums as strings) |
| `src/Klasly.Web/Repositories/{Term,Subject,Account,Class}Repository.cs` + interfaces | added | One line per API endpoint |
| `src/Klasly.Web/Services/{Term,Subject,Account,Class}Service.cs` + interfaces | added | View-model mapping; `AccountService` and `ClassService` combine calls for dropdowns |
| `src/Klasly.Web/ViewModels/{Term,Subject,Account}FormViewModel.cs`, `ParentLinksViewModel.cs`, `ClassViewModels.cs` | added | Form fields with the API's limits; property names match the API requests |
| `src/Klasly.Web/Areas/Admin/Controllers/{Home,Terms,Subjects,Accounts,Classes}Controller.cs` | added | `[Area("Admin"), Authorize(Roles = "Admin")]`; every POST has `[ValidateAntiForgeryToken]` |
| `src/Klasly.Web/Areas/Admin/Views/**` | added | Admin home list; Index + shared Form per resource; `Accounts/_ParentLinks`; `Classes/Details` |
| `src/Klasly.Web/Views/Shared/ApiUnavailable.cshtml` | added | "Klasly can't reach the server right now." |
| `src/Klasly.Web/Views/Shared/_Layout.cshtml` | modified | Admin nav (Terms, Subjects, Accounts, Classes) with current page marked |
| `src/Klasly.Web/Controllers/HomeController.cs` | modified | Admin redirect uses the Area route |
| `src/Klasly.Web/wwwroot/js/site.js` | modified | `data-confirm` forms ask before submitting (Deactivate, Drop, Unlink) |
| `src/Klasly.Web/wwwroot/css/site.css` | modified | Hide empty validation summary; tabular numerals in tables |
| `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` | modified | Status Done, success criteria and tasks ticked |

## Key code
API error → page, in one place (`Infrastructure/ApiExceptionFilter.cs`):
```csharp
case ApiException { StatusCode: StatusCodes.Status401Unauthorized }:
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    tempDataFactory.GetTempData(http)["Error"] = "Your session ended, please log in again.";
    ...
case HttpRequestException or TaskCanceledException when !http.RequestAborted.IsCancellationRequested:
    context.Result = new ViewResult { ViewName = "ApiUnavailable", StatusCode = 503 };
```
Repository methods are one line (`Repositories/ClassRepository.cs`):
```csharp
public Task<EnrollmentResponse> EnrollAsync(EnrollmentRequest request, CancellationToken ct) =>
    http.PostApiAsync<EnrollmentResponse>("api/enrollments", request, ct);
```
Form error handling (`Areas/Admin/Controllers/TermsController.cs`):
```csharp
catch (ApiException ex) when (ex.IsFormError)   // 400 or 409
{
    ModelState.AddApiErrors(ex, model);
    return View("Form", model);
}
```

## Design notes
- **Expired login message:** the cookie expires with the JWT, so the cookie handler redirects before the API is ever called. `OnRedirectToLogin` adds "Your session ended…" when the browser still sent a `Klasly.Auth` cookie; a never-logged-in visitor sees no message.
- **Error placement:** `AddApiErrors` matches API error keys against the view model's properties (not ModelState entries), because unposted fields have no ModelState entry. Verified with the API's `Password` error on account create.
- **`DateOnly` dates** need `[DataType(DataType.Date)]`, or the tag helper renders a text box with `8/17/2026`.
- **`/impeccable` (Operate mode, refinement of the incumbent Bootstrap look):** Admin home changed from a card grid to a list; row-level destructive actions changed from red outlined buttons to quiet `link-danger` buttons; tabular numerals in tables. Detector: no findings. Checked at desktop and 375 px (no horizontal scroll; nav wraps).

## Database changes
None. Test rows were added through the website during the smoke test (all fictional): term "2nd Sem 2026-27" (inactive), subject "CPE 401", accounts `m5.*.205344@klasly.test` (student, teacher, parent, admin), class "CPE 401 – M5" with one enrollment. The seed term "1st Sem 2026-27" was accidentally edited by a test script and restored to its original name and dates through the Edit page.

## How it was verified
- `dotnet build Klasly.slnx` → **0 warnings, 0 errors**. `dotnet list package` → every Microsoft package 10.0.14. `Klasly.Web.csproj` has no package or project reference; no connection string or key in its settings.
- No test project (SPEC assumption 12). Manual smoke test over HTTPS with curl and the browser pane — all 12 SPEC steps pass:
  1. Wrong password message, email kept; admin → `/Admin`; logout. 2. Teacher/parent welcome; student blocked with no cookie. 3. Teacher → Access denied; logged out → login → back to the page. 4. Cookie `secure; samesite=lax; httponly`, session, ~1 KB; `document.cookie` empty. 5. Term create/edit/activate (one Active badge), end-before-start message, duplicate name `409` message. 6. Subject create/edit; units 11 and duplicate code messages. 7. One account per role with only that role's fields; missing student number / duplicate email messages; deactivate blocks login; reactivate + new password; no Deactivate on own account; API refuses self-deactivate. 8. Link → listed; link again → message; unlink → gone. 9. Class create with dropdowns (active term preselected); duplicate message; term filter and "All terms". 10. Enroll → Active; drop → Dropped; re-enroll → Active on the same enrollment row. 11. API stopped → friendly 503 page, error logged. 12. 1-minute JWT → after 70 s, login page with "Your session ended" and `ReturnUrl`; rejected token (API restarted with another key) → same message.

## Known limits / TODO
- Classes whose teacher was deactivated: the Edit dropdown lists only active teachers, so the admin must pick a new one (marked `shortcut:` in `ClassService`).
- A login that expires while filling in a form loses the typed values (redirect to login).
- Tasks 4–6 and 7–8 were committed as two commits instead of five (they edit the same files).
- No automated tests yet (Milestone 14).
