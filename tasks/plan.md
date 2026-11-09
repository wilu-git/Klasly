# Implementation Plan: Milestone 5 — Web MVC login + Admin pages

> Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-09 with the recommended answers to the 3 open questions). Task details, acceptance criteria and verification steps are in [`todo.md`](./todo.md).
> Milestone 4's plan is in git history (commit `f87ee09`).

## Overview

Turn the bare `Klasly.Web` template into the website's login and Admin portal. Each Admin resource is one **vertical slice**: API shapes → API-backed repository → service → controller → views, so after every task there is a page you can open and click. The Web never touches the database: every read and write goes through the Milestone 4 API. No API, database or package changes.

## Facts from the code that shape the plan

- The API serializes enums as **strings only** (`JsonStringEnumConverter(allowIntegerValues: false)`, `src/Klasly.API/Program.cs:16`). The Web's copied shapes use `string Role` / `string Status`, so no enum copies are needed.
- `[ApiController]` validation errors are keyed by **C# property name** (`Code`, `Units`, `StartDate`…). Form view models use the **same property names** as the API requests (`SubjectRequest`, `TermRequest`, `AccountRequestBase`, `ClassRequest`, `LinkStudentRequest` in `src/Klasly.Core/DTOs/`), so each API error lands next to its field. Task 2 checks this with one real error before the pattern is copied.
- `POST /api/auth/login` returns `LoginResponse(Token, ExpiresAtUtc, UserId, FullName, Role)`, or `401` with "Invalid email or password." (`src/Klasly.API/Controllers/AuthController.cs`).
- The API runs on `https://localhost:7256`, the Web on `https://localhost:7144`. `Klasly.Web` is still the template; its csproj already has an empty `ViewModels/` folder.

## Dependency graph

```
Task 1  Login + logout end to end (cookie auth, layout, home redirect)
   │
   └── Task 2  Authenticated API plumbing + Admin home + Terms  ← sets the pattern
          │
          ├── Task 3  Subjects
          │
          ├── Task 4  Accounts: list + create
          │      ├── Task 5  Accounts: edit / deactivate / password reset
          │      └── Task 6  Parent links            (needs Task 5's Edit page)
          │
          └── Task 7  Classes list + create/edit   ◄── needs Subjects (3), teachers (4), Terms (2)
                 │
                 └── Task 8  Class details: enroll / drop   ◄── needs students (4)
                        │
                        └── Task 9  /impeccable, full smoke test, reports, commit
```

Tasks 3–8 all edit `Program.cs` (one client registration each) and the layout nav, so they run **one after another in one session**, not in parallel.

## Architecture decisions

| Decision | Why |
|---|---|
| Cookie auth: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)`. Cookie name `Klasly.Auth`, `HttpOnly`, `SecurePolicy.Always`, `SameSite.Lax`, `SlidingExpiration = false`, `LoginPath = /Account/Login`, `AccessDeniedPath = /Account/AccessDenied` | Spec assumption 2. Built into ASP.NET Core 10, no package. |
| At login, `AuthService` builds a `ClaimsPrincipal` (NameIdentifier = userId, Name = fullName, Role = role) plus `AuthenticationProperties { ExpiresUtc = ExpiresAtUtc, IsPersistent = false }`, and stores the JWT with `StoreTokens([new() { Name = "access_token", Value = token }])`. The controller calls `HttpContext.SignInAsync` | The login ends when the JWT ends. A session cookie also dies when the browser closes, which is safer on shared lab PCs. Signing in is an MVC concern, so it stays in the controller. |
| `AuthService` refuses the Student role ("Students use the Klasly mobile app.") without signing in | Decision 1. |
| `AuthRepository.LoginAsync` returns `null` on `401` instead of throwing | A wrong password is a normal form result. If it threw, the filter would treat it as "session ended". |
| `BearerTokenHandler : DelegatingHandler` reads `HttpContext.GetTokenAsync("access_token")` through `IHttpContextAccessor` and sets `Authorization: Bearer …` | One place adds the header. Controllers never touch `HttpClient` (guide §11). |
| One local helper in `Program.cs` registers each repository: `AddHttpClient<IXRepository, XRepository>(c => { c.BaseAddress = new(apiBaseUrl); c.Timeout = TimeSpan.FromSeconds(15); }).AddHttpMessageHandler<BearerTokenHandler>()` | Typed clients (CLAUDE.md §4.1). 15 s means a dead API shows the error page quickly instead of hanging for the default 100 s. |
| `ApiResponse.ReadAsync<T>(response, ct)` and `ApiResponse.EnsureAsync(response, ct)`: success → read the JSON; failure → read the problem-details (`title`, `detail`, `errors`) and throw `ApiException(status, detail, errors)` | Each repository method is one line (spec code style). |
| `ApiExceptionFilter` (global MVC exception filter): `401` → `SignOutAsync`, TempData "Your session ended, please log in again.", redirect to login (`returnUrl` = current URL on GET only). `403` → AccessDenied. `404` → `NotFound()`. `HttpRequestException` / `TaskCanceledException` → log + `ApiUnavailable` view | Spec "How API answers are shown": handled in one place. Controllers only catch `ex.IsFormError` (400 / 409) on form posts. |
| `ModelStateExtensions.AddApiErrors(this ModelStateDictionary, ApiException)`: each `errors` entry goes to its key; `detail` goes to the summary key `""` | Field messages next to the inputs; rule and duplicate messages in the red summary box. |
| Admin Area: `[Area("Admin"), Authorize(Roles = "Admin")]` on each Admin controller. Route `{area:exists}/{controller=Home}/{action=Index}/{id?}` is mapped before the default route | Spec assumption 6. Teacher and Parent Areas come later. |
| One `Form.cshtml` per resource for both Create and Edit. The view model's `Id` (`null` = create) picks the form's target action and the page title | Half the views. |
| The account create form takes its fields from `?role=`: `Role` is a hidden field and the view has `@if (Model.Role == "Student")` blocks. Edit reuses the view without role and password and adds `IsActive` + `NewPassword` | Spec: no JavaScript. |
| Services combine calls for views: `ClassService.GetFormAsync` (subjects + active teachers + terms), `ClassService.GetDetailsAsync` (class + enrollments + active students not Active in the class), `AccountService.GetEditAsync` (account + linked students + student dropdown when the account is a Parent) | Guide §3: MVC services coordinate and shape data. No business rules in the Web. |
| Classes filter: no `termId` → active term (all terms if none is active); the "All terms" option sends `termId=0` | Decision 3. |
| API shapes in `Models/Api/`, one file per resource holding its request + response records. Form view models in `ViewModels/` | `Klasly.Web` may not reference `Klasly.Core` (CLAUDE.md §3.4). |
| Delete the template Privacy view and its action and links. The layout gets the Klasly brand, role-based nav, the user's name, a Logout POST form, and an `_Alerts` partial (TempData `Success` / `Error`) | Spec project structure. |

## Task list

### Phase 1: Login
- [x] Task 1: Login and logout end to end (M)

### Phase 2: Pattern slice and simple resources
- [x] Task 2: Authenticated API plumbing + Admin home + Terms (M–L)
- [x] Task 3: Subjects (S)

### Checkpoint A — after Tasks 1–3
- [ ] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [ ] SPEC smoke steps 1–6, 11 and 12 pass
- [ ] Team looks at the layout and form style before more pages copy it

### Phase 3: Accounts
- [ ] Task 4: Accounts — list + create (M)
- [ ] Task 5: Accounts — edit, deactivate, password reset (S)
- [ ] Task 6: Parent links (S)

### Checkpoint B — after Tasks 4–6
- [ ] SPEC smoke steps 7 and 8 pass
- [ ] No page ever shows raw JSON or a stack trace

### Phase 4: Classes and enrollments
- [ ] Task 7: Classes — list + create/edit (M)
- [ ] Task 8: Class details — enroll, drop (S–M)

### Checkpoint C — after Tasks 7–8
- [ ] SPEC smoke steps 9 and 10 pass

### Phase 5: Wrap-up
- [ ] Task 9: `/impeccable`, full smoke test, package check, reports, commit (S)

### Checkpoint D — complete
- [ ] Every SPEC.md success criterion ticked
- [ ] `Klasly.Web.csproj` has no EF Core, `Klasly.Core` or `Klasly.Infrastructure` reference; `dotnet list package` shows no Microsoft package below 10.x
- [ ] Reports in `docs/reports/`, `INDEX.md` updated, committed to `dev`

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| API field-error keys don't match the view model names, so messages land only in the summary | Med: worse UX on every form | Same property names as the API requests; checked in Task 2 before the pattern is copied. |
| JWT inside the cookie makes it bigger than 4 KB | Low | The token is about 600 bytes, so the encrypted ticket stays well under 4 KB, and cookie auth splits big cookies into chunks anyway. Size checked in smoke step 4. |
| Dev HTTPS certificate not trusted, so every API call fails | Med: looks like "API down" | HOW-TO-USE says to run `dotnet dev-certs https --trust`; the error page says "can't reach the server", not a crash. |
| A class whose teacher was deactivated: the Edit dropdown doesn't list them | Low | The API returns `400` and its message is shown. Noted in the report. |
| Open redirect through `returnUrl` | High: phishing | Follow only `Url.IsLocalUrl(returnUrl)`; otherwise go to the role's home page. |
| Expired login on a POST loses the typed form | Low | The JWT lasts 60 min; the user is sent to the login page with the message. Accepted for this milestone. |

## Out of scope (from SPEC.md)

Teacher and Parent portals (Milestones 7, 9, 10, 13), dashboards with counts, search and paging, change-my-own-password, API or database changes, test project (Milestone 14), new packages, extra JavaScript beyond `confirm()` and the template's validation scripts.

## Open questions

None. The three spec questions were answered on 2026-11-09 with the recommended options (see SPEC.md "Decisions").
