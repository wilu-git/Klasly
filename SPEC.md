# Spec: Milestone 5 — Web MVC login + Admin pages

> Status: **Done — built 2026-11-09** (all 12 smoke steps pass). Reports: `docs/reports/2026-11-09-2034-milestone-5-task-1-login/`, `docs/reports/2026-11-09-2101-milestone-5-web-admin/`. Approved 2026-11-09 with the recommended answers to the 3 open questions (see "Decisions"). Plan: `tasks/plan.md` · Tasks: `tasks/todo.md`. Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` → `CLAUDE.md` → this spec.
> Milestone 4's spec (the API contract these pages call) is in git history (commit `f87ee09`) and in `docs/reports/2026-11-09-2009-milestone-4-admin-crud/`.

## Assumptions (correct these before we build)

1. **The website uses the API for everything.** `Klasly.Web` keeps zero database access (CLAUDE.md §3.4, guide §11). No `Klasly.Core` reference, so the Web project has **its own copies of the API's JSON shapes** (small records in `Models/Api/`). The duplication is the price of the rule.
2. **Login = built-in cookie authentication, with the JWT stored inside the cookie.** On login the Web calls `POST /api/auth/login`, then signs the user in with ASP.NET Core's cookie handler (`AddAuthentication().AddCookie()`, no new package). The JWT is saved in the encrypted auth cookie with `StoreTokens`, and a `DelegatingHandler` adds `Authorization: Bearer <jwt>` to every API call. Cookie flags: `HttpOnly`, `Secure` (always), `SameSite=Lax`. The Web never needs the JWT signing key.
   *Why Lax, not Strict:* Strict drops the login when someone opens a Klasly link from email or chat; anti-forgery tokens already stop cross-site form posts.
3. **No token refresh.** The cookie expires at the same moment as the JWT (`ExpiresAtUtc`, 60 min today, not sliding). If the API answers `401` (expired or deactivated user), the Web signs the user out and sends them to the login page with a "Your session ended, please log in again." message and a `returnUrl`.
4. **Who can log in to the website:** Admin, Teacher, Parent. **Students get "Students use the Klasly mobile app."** and are not signed in — the student screens are Android only (CLAUDE.md §3.6).
5. **Teacher and Parent portals are not part of this milestone** (Milestones 7, 9, 10, 13). They can log in and land on a simple welcome page: "Hi, {name}. Your {role} pages are coming soon." That proves login works for every web role.
6. **Admin pages live in an MVC Area named `Admin`** (`/Admin/Terms`, `/Admin/Classes`…). Teacher and Parent get their own Areas later, so each portal stays in its own folder. `[Authorize(Roles = "Admin")]` on the Area's controllers is only for a friendly redirect — **the API is the real guard** (guide §27.5).
7. **Every API endpoint from Milestone 4 gets a page or a button. Nothing more.** No dashboards with counts, no search, no paging, no CSV import, no change-my-own-password page.
8. **Layers follow guide §2:** Controller → Service → API-backed Repository → typed `HttpClient` → Web API. Repositories only make HTTP calls and turn errors into an `ApiException`. Services shape data for views and combine calls (for example the class form needs subjects + teachers + terms). Most services will be thin; that's expected.
9. **Forms post, then redirect** (Post/Redirect/Get). Success shows a green message on the next page (`TempData`). Failures stay on the form and show the API's message. HTML forms can only send GET/POST, so "Deactivate", "Drop", "Unlink" and "Activate" are small POST forms; the repository turns them into the right `DELETE` / `POST` API call.
10. **Validation runs twice on purpose.** View models copy the API's simple limits (`[Required]`, `[StringLength]`, `[Range]`) so the user sees mistakes before submitting. The API still decides; its `400`/`409` messages are shown on the form.
11. **Destructive buttons ask first** with the browser's `confirm()` dialog ("Drop Bianca Reyes from CPE 301 – A?"). Nothing is ever hard-deleted except a parent–student link (Milestone 4 decision).
12. **No test project** (same as Milestones 3–4). Manual browser smoke test, then `/impeccable` on the views. Automated role tests are Milestone 14.
13. **No database changes, no API changes.** If a page turns out to need an API change, stop and ask.

## Objective

Give the school admin a website to set up a term without Swagger: log in, create the semester, the subject catalog, the user accounts (and link parents to children), the class sections, and the enrollments. The same login page also serves teachers and parents, so the next milestones only add pages, not login.

Functional requirements covered: guide §27.9 **#1** (log in by role, web side) and **#2** (admin manages terms, subjects, classes, enrollments), plus **#10** (show a clear error message when an operation fails).

### User stories
- As **any web user**, I log in with email + password, see my name in the top bar, and log out.
- As **any web user**, a wrong password shows "Invalid email or password." and keeps the email I typed.
- As the **admin**, I land on an Admin home page that links to Terms, Subjects, Accounts and Classes.
- As the **admin**, I create "1st Sem 2026-27", edit it, mark it active, and see which term is active at a glance.
- As the **admin**, I add and edit subjects.
- As the **admin**, I filter accounts by role, create a teacher / student / parent / admin with only the fields that role needs, edit them, reset a password, deactivate and reactivate them.
- As the **admin**, on a parent's page I link and unlink their children.
- As the **admin**, I create and edit classes by picking subject, teacher and term from dropdowns, and filter the class list by term.
- As the **admin**, on a class's page I see who is enrolled (Active and Dropped), enroll a student from a dropdown, and drop a student.
- As the **admin**, when I type a duplicate (email, student number, subject code…) the form stays filled in and shows the API's message naming the value.

## Pages

All Admin pages need the Admin role. Not logged in → login page with `returnUrl`. Logged in as Teacher/Parent → Access denied page.

| URL | What it shows / does | API calls |
|---|---|---|
| `GET /Account/Login` | Email + password form | — |
| `POST /Account/Login` | Logs in; Admin → `/Admin`, Teacher/Parent → `/`; or back to `returnUrl` if it is a local URL | `POST /api/auth/login` |
| `POST /Account/Logout` | Signs out → login page | — |
| `GET /Account/AccessDenied` | "You don't have access to that page." + link home | — |
| `GET /` | Not logged in → login. Admin → `/Admin`. Teacher/Parent → welcome page (assumption 5) | — |
| `GET /Admin` | Admin home: four links (Terms, Subjects, Accounts, Classes) with one-line descriptions | — |
| **Terms** | | |
| `GET /Admin/Terms` | Table: name, start, end, Active badge; buttons Edit / Make active / Deactivate | `GET /api/terms` |
| `GET·POST /Admin/Terms/Create`, `/Edit/{id}` | Form: name, start date, end date | `POST` / `GET`+`PUT /api/terms/{id}` |
| `POST /Admin/Terms/Activate/{id}` | Makes it the active term | `POST /api/terms/{id}/activate` |
| `POST /Admin/Terms/Deactivate/{id}` | Makes it not active | `DELETE /api/terms/{id}` |
| **Subjects** | | |
| `GET /Admin/Subjects` | Table: code, title, units; Edit button | `GET /api/subjects` |
| `GET·POST /Admin/Subjects/Create`, `/Edit/{id}` | Form: code, title, units | `POST` / `GET`+`PUT /api/subjects/{id}` |
| **Accounts** | | |
| `GET /Admin/Accounts?role=` | Role tabs (All / Admin / Teacher / Student / Parent); table: name, email, role, ID no. (student no. or employee no.), Active/Inactive; Edit button. "New account" button per role | `GET /api/accounts?role=` |
| `GET·POST /Admin/Accounts/Create?role=Student` | Form with common fields (name, email, password) **plus only the chosen role's fields** — server-rendered from `role`, no JavaScript | `POST /api/accounts` |
| `GET·POST /Admin/Accounts/Edit/{id}` | Same fields minus role and password; `Active` checkbox; optional "New password" box (blank = unchanged) | `GET`+`PUT /api/accounts/{id}` |
| `POST /Admin/Accounts/Deactivate/{id}` | Deactivate (button hidden on your own account; the API also refuses) | `DELETE /api/accounts/{id}` |
| *(Parent accounts only, on the Edit page)* | "Linked students" table with Unlink buttons + a "Link a student" form (student dropdown, relationship, receives alerts checkbox) | `GET·POST /api/accounts/{id}/students`, `GET /api/accounts?role=Student` |
| `POST /Admin/Accounts/LinkStudent/{id}`, `/UnlinkStudent/{id}?studentId=` | Link / unlink, then back to the Edit page | `POST` / `DELETE /api/accounts/{id}/students[/{studentId}]` |
| **Classes + Enrollments** | | |
| `GET /Admin/Classes?termId=` | Term dropdown filter (defaults to the active term); table: subject code + title, section, teacher, schedule, room, enrolled count; Edit / Students buttons | `GET /api/classes?termId=`, `GET /api/terms` |
| `GET·POST /Admin/Classes/Create`, `/Edit/{id}` | Form: subject, teacher (active teachers only), term (dropdowns), section, schedule, room | `GET /api/subjects`, `GET /api/accounts?role=Teacher`, `GET /api/terms`, `POST`/`PUT /api/classes` |
| `GET /Admin/Classes/Details/{id}` | Class header + enrollment table (student no., name, Active/Dropped, Drop button) + "Enroll a student" form (dropdown of active students not already Active in this class) | `GET /api/classes/{id}`, `GET /api/classes/{id}/enrollments`, `GET /api/accounts?role=Student` |
| `POST /Admin/Classes/Enroll/{id}` | Enroll (re-enrolling a Dropped student works too: API returns `200`) | `POST /api/enrollments` |
| `POST /Admin/Classes/Drop/{id}?enrollmentId=` | Drop | `DELETE /api/enrollments/{enrollmentId}` |

Every `POST` action has `[ValidateAntiForgeryToken]` (CLAUDE.md §3.4).

### How API answers are shown

| API answer | What the user sees |
|---|---|
| `2xx` | Redirect to the list (or back to the detail page) with a green "Saved." / "Term activated." message |
| `400` with `errors` (field validation) | Each message next to its field (same property names); unknown keys go in the summary at the top of the form |
| `400` / `409` with `detail` | The `detail` text in a red box at the top of the form; the form keeps what was typed |
| `401` | Signed out → login page, "Your session ended, please log in again." |
| `403` | Access denied page |
| `404` | Standard Not Found page |
| API down / timeout | Error page: "Klasly can't reach the server right now. Try again in a minute." (logged; no stack trace shown) |

`401`, `403`, `404` and "API down" are handled in **one** place (an exception filter on `ApiException` / `HttpRequestException`), so controllers only catch the `400`/`409` case on form posts.

## Commands

```bash
dotnet build Klasly.slnx
dotnet list Klasly.slnx package
dotnet run --project src/Klasly.API --launch-profile https   # API on https://localhost:7256
dotnet run --project src/Klasly.Web --launch-profile https   # Website on https://localhost:7144
```
`Api:BaseUrl` = `https://localhost:7256/` in `src/Klasly.Web/appsettings.Development.json` (not a secret). The dev HTTPS certificate must be trusted (`dotnet dev-certs https --trust`). No migration.

## Project structure (files this milestone touches)

```
src/Klasly.Web/
  Program.cs                      (modified) cookie auth, typed HttpClients + bearer handler,
                                  services/repositories, exception filter, Area route
  appsettings.Development.json    (modified) Api:BaseUrl
  Models/Api/                     (new) JSON shapes copied from the API: Login*, Term*, Subject*,
                                  Class*, Enrollment*, Account*, LinkedStudent* (one file per resource)
  Infrastructure/
    BearerTokenHandler.cs         (new) DelegatingHandler: reads the JWT from the auth cookie, adds the header
    ApiException.cs               (new) status code + detail + field errors from problem-details
    ApiResponse.cs                (new) one helper: success → read JSON; failure → throw ApiException
    ApiExceptionFilter.cs         (new) 401 → sign out + login, 403 → AccessDenied, 404 → NotFound,
                                  API unreachable → error page
  Repositories/ (+ Interfaces/)   (new) AuthRepository, TermRepository, SubjectRepository,
                                  AccountRepository (incl. links), ClassRepository (incl. enrollments)
  Services/ (+ Interfaces/)       (new) AuthService (login → claims), TermService, SubjectService,
                                  AccountService, ClassService
  Controllers/
    AccountController.cs          (new) Login, Logout, AccessDenied
    HomeController.cs             (modified) redirect by role; welcome page; remove template Privacy
  Areas/Admin/
    Controllers/                  (new) HomeController, TermsController, SubjectsController,
                                  AccountsController, ClassesController
    Views/                        (new) one Index + one shared create/edit form per resource,
                                  Accounts/_ParentLinks partial, Classes/Details
  ViewModels/                     (new) LoginViewModel, TermFormViewModel, SubjectFormViewModel,
                                  AccountFormViewModel, ClassFormViewModel, ClassDetailsViewModel …
  Views/Account/                  (new) Login, AccessDenied
  Views/Shared/_Layout.cshtml     (modified) Klasly brand, nav by role, user name + Logout button,
                                  TempData success / error messages
  Views/Home/Privacy.cshtml       (deleted) template leftover
```

No new packages — cookie auth, `IHttpClientFactory` and `System.Net.Http.Json` all ship with ASP.NET Core 10. No generic repository, AutoMapper, or MediatR (guide §21).

## Code style

Same as Milestones 3–4: file-scoped namespaces, primary constructors, `async` + `CancellationToken` all the way down. Repositories throw `ApiException`; controllers only handle the form case:

```csharp
namespace Klasly.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public class SubjectsController(ISubjectService subjects) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Form", model);

        try
        {
            await subjects.CreateAsync(model, ct);
        }
        catch (ApiException ex) when (ex.IsFormError)   // 400 or 409
        {
            ModelState.AddApiErrors(ex);
            return View("Form", model);
        }

        TempData["Success"] = $"Subject {model.Code} saved.";
        return RedirectToAction(nameof(Index));
    }
}
```

```csharp
namespace Klasly.Web.Repositories;

public class SubjectRepository(HttpClient http) : ISubjectRepository
{
    public async Task<SubjectResponse> CreateAsync(SubjectRequest request, CancellationToken ct) =>
        await ApiResponse.ReadAsync<SubjectResponse>(
            await http.PostAsJsonAsync("api/subjects", request, ct), ct);
}
```

UI: Bootstrap from the template, one consistent admin layout (tables with a header action button, forms in a single column, status as badges). Dates shown as `MMM d, yyyy`. `/impeccable` runs on every view before the milestone is called done.

## Testing strategy

- **Build:** `dotnet build` with 0 errors and 0 new warnings; `dotnet list package` shows no Microsoft package below 10.x, and `Klasly.Web` has **no** package or project reference to EF Core, `Klasly.Core` or `Klasly.Infrastructure`.
- **Manual browser smoke test** with the API running on LocalDB and the Milestone 2 seed (`admin@klasly.test` / `Klasly@123`):
  1. Wrong password → "Invalid email or password.", email kept. Admin login → `/Admin`. Name shows in the top bar. Logout → login page.
  2. Seeded teacher and parent log in → welcome page. Seeded student → "Students use the Klasly mobile app." and not signed in.
  3. Teacher opens `/Admin/Terms` → Access denied. Logged out, `/Admin/Subjects` → login, then back to `/Admin/Subjects` after logging in.
  4. Browser dev tools: the auth cookie is `HttpOnly`, `Secure`, `SameSite=Lax`; the JWT is not readable from page JavaScript (`document.cookie` doesn't show it).
  5. Terms: create, edit, make active → only that row has the Active badge. End date before start date → error on the form.
  6. Subjects: create; duplicate code → the API's "already used" message, form still filled in.
  7. Accounts: create one of each role (each form shows only that role's fields); student without student number → error; duplicate email → `409` message. Deactivate the new teacher → their login fails. Reactivate + reset password → login works with the new password. Your own account has no Deactivate button.
  8. Parent links: link a student → appears in the list; link again → message; unlink → gone.
  9. Classes: create with dropdowns; duplicate subject + section + term → message. Term filter changes the list.
  10. Enrollments: enroll → appears Active; drop → Dropped; enroll again → Active (same row).
  11. Stop the API, click any Admin page → friendly "can't reach the server" page, no stack trace.
  12. Set `Jwt:ExpiryMinutes` to 1 on the API, log in, wait 2 minutes, click a page → back to login with "Your session ended".
- No automated tests (assumption 12).

## Boundaries

- **Always:** Controller → Service → API-backed Repository; `[ValidateAntiForgeryToken]` on every POST; show the API's message on failure; only redirect to local `returnUrl`s (`Url.IsLocalUrl`); `async` + `CancellationToken`; run `dotnet build`; `/impeccable` on views; write the three reports in `docs/reports/` when the milestone is built.
- **Ask first:** any change to `Klasly.API` or the database; any new package; adding JavaScript beyond `confirm()` and the template's validation scripts; any page or feature not in the Pages table.
- **Never:** reference EF Core, `Klasly.Core` or `Klasly.Infrastructure` from `Klasly.Web`; put a connection string or the JWT key in the Web project; store the JWT in `localStorage`, a non-HttpOnly cookie, or the page HTML; inject `HttpClient` into controllers or views; re-implement API business rules in the Web; turn on CORS; show stack traces or raw problem-details JSON to users.

## Success criteria

- [x] All 12 smoke-test steps behave as written.
- [x] Every Milestone 4 admin endpoint is reachable from a page or button, and nothing else was added.
- [x] The JWT only ever lives inside the HttpOnly, Secure, SameSite=Lax auth cookie and the server-side `Authorization` header.
- [x] A failed save never loses what the user typed and always shows a human-readable message.
- [x] `Klasly.Web` builds with no database or `Klasly.Core` reference.
- [x] `/impeccable` run on the views; findings fixed or listed in the report.
- [x] Reports written; `docs/reports/INDEX.md` updated.

## Decisions (were open questions — approved 2026-11-09)

1. **Students on the website:** blocked with "Students use the Klasly mobile app." and not signed in (assumption 4).
2. **Teachers and parents in this milestone:** they log in and see a "coming soon" welcome page (assumption 5).
3. **Default term on the Classes list:** the active term (all terms if none is active); an "All terms" option shows every class.
