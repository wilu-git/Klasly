# Milestone 5 — Task list

> Plan: [`plan.md`](./plan.md) · Spec: [`SPEC.md`](../SPEC.md)
> Build command for every task: `dotnet build Klasly.slnx`. No test project, so each task is checked in the browser: API on `https://localhost:7256` (`dotnet run --project src/Klasly.API --launch-profile https`), website on `https://localhost:7144` (`dotnet run --project src/Klasly.Web --launch-profile https`). Seed logins: `admin@klasly.test` / `Klasly@123` (teachers, students and parents use the same password).
> "Step N" means smoke-test step N in SPEC.md → Testing strategy.
> Every POST action gets `[ValidateAntiForgeryToken]`. Every Admin controller gets `[Area("Admin"), Authorize(Roles = "Admin")]`.

---

## Task 1: Login and logout end to end

**Description:** Anyone with a web role can log in through the API and log out. The JWT is kept inside the HttpOnly auth cookie. This also sets up the API error helper that every later task uses.

**Acceptance criteria:**
- [x] `Api:BaseUrl` = `https://localhost:7256/` in `appsettings.Development.json`. `Models/Api/LoginDtos.cs` (`LoginRequest`, `LoginResponse`) and `Models/Api/ProblemDetailsDto.cs` (`Title`, `Detail`, `Errors`).
- [x] `ApiException(int StatusCode, string Message, IDictionary<string,string[]> Errors)` with `IsFormError` (400 or 409). `ApiResponse.ReadAsync<T>` / `EnsureAsync` throw it on any non-success.
- [x] `AuthRepository.LoginAsync` returns `null` on `401` and throws `ApiException` on anything else that isn't a success.
- [x] `AuthService.LoginAsync` returns the principal + properties (claims NameIdentifier / Name / Role, `ExpiresUtc = ExpiresAtUtc`, `IsPersistent = false`, JWT stored as `access_token`), or an error message: "Invalid email or password." / "Students use the Klasly mobile app."
- [x] Cookie auth in `Program.cs` with the cookie options from the plan; `UseAuthentication()` before `UseAuthorization()`.
- [x] `AccountController`: `Login` GET/POST (keeps the typed email on failure; follows `returnUrl` only when `Url.IsLocalUrl`, else Admin → `/Admin`, others → `/`), `Logout` POST, `AccessDenied` GET. `LoginViewModel` with `[Required, EmailAddress]` email and `[Required]` password.
- [x] `HomeController.Index`: anonymous → login; Admin → `/Admin`; Teacher/Parent → welcome page "Hi, {name}. Your {role} pages are coming soon." Privacy action and view deleted.
- [x] `_Layout`: brand "Klasly", the user's name + Logout button when signed in, `_Alerts` partial for TempData `Success` / `Error`. No Privacy links.
- [x] The TempData message "Your session ended, please log in again." shows on the login page when present (the filter in Task 2 sets it).

**Verification:**
- [x] Build succeeds
- [x] Step 1 (wrong password, admin login, name in the top bar, logout). `/Admin` gives 404 for now (Task 2 adds it).
- [x] Step 2 (teacher and parent → welcome; student → blocked and not signed in)
- [x] Step 4 (cookie flags; `document.cookie` doesn't show `Klasly.Auth`)

**Dependencies:** None

**Files likely touched:**
- `src/Klasly.Web/Program.cs`, `appsettings.Development.json`
- `src/Klasly.Web/Models/Api/LoginDtos.cs`, `Models/Api/ProblemDetailsDto.cs` (new)
- `src/Klasly.Web/Infrastructure/ApiException.cs`, `Infrastructure/ApiResponse.cs` (new)
- `src/Klasly.Web/Repositories/AuthRepository.cs` + `Interfaces/IAuthRepository.cs`, `Services/AuthService.cs` + `Interfaces/IAuthService.cs` (new)
- `src/Klasly.Web/Controllers/AccountController.cs` (new), `Controllers/HomeController.cs`
- `src/Klasly.Web/ViewModels/LoginViewModel.cs` (new)
- `src/Klasly.Web/Views/Account/Login.cshtml`, `AccessDenied.cshtml`, `Views/Shared/_Alerts.cshtml` (new); `Views/Shared/_Layout.cshtml`, `Views/Home/Index.cshtml`; `Views/Home/Privacy.cshtml` (deleted)

**Estimated scope:** M (about 15 files, most of them small)

---

## Task 2: Authenticated API plumbing + Admin home + Terms

**Description:** Calls that carry the JWT, the single place that handles 401 / 403 / 404 / API down, and the first Admin slice. Terms sets the pattern every other resource copies.

**Acceptance criteria:**
- [ ] `BearerTokenHandler` adds `Authorization: Bearer <access_token>` when the user is signed in. Registered as transient, with `AddHttpContextAccessor()`.
- [ ] A local helper in `Program.cs` registers typed clients with base address `Api:BaseUrl`, a 15 s timeout and the bearer handler. Terms is the first client to use it.
- [ ] `ApiExceptionFilter` registered globally: `401` → sign out + TempData message + redirect to login (with `returnUrl` on GET only); `403` → AccessDenied; `404` → `NotFound()`; `HttpRequestException` / `TaskCanceledException` → logged + `Views/Shared/ApiUnavailable.cshtml` ("Klasly can't reach the server right now. Try again in a minute.").
- [ ] `ModelStateExtensions.AddApiErrors` puts each `errors` entry on its key and `detail` on `""`.
- [ ] Area route mapped before the default route. `Areas/Admin/Controllers/HomeController` + view: four links (Terms, Subjects, Accounts, Classes) with one-line descriptions. Layout shows the Admin nav for the Admin role. `Areas/Admin/Views/_ViewImports.cshtml` + `_ViewStart.cshtml`.
- [ ] `Models/Api/TermDtos.cs` (`TermRequest` with `DateOnly?` dates, `TermResponse`); `ITermRepository` / `TermRepository` (list, get, create, update, activate, deactivate); `ITermService` / `TermService`.
- [ ] `TermsController`: `Index`, `Create` GET/POST, `Edit` GET/POST, `Activate` POST, `Deactivate` POST (with `confirm()`). Success → redirect to Index with a TempData message.
- [ ] `TermFormViewModel` (`Id?`, `Name` [Required, StringLength 50], `StartDate`, `EndDate` [Required]); `Index.cshtml` (table with an Active badge and buttons) + `Form.cshtml` (validation summary + field messages).

**Verification:**
- [ ] Build succeeds
- [ ] Step 3 (teacher → Access denied; logged out → login → back to the page)
- [ ] Step 5 (create, edit, activate; end before start → message)
- [ ] Key mapping check: turn client validation off (remove `_ValidationScriptsPartial` for the check, or post with dev tools) and submit a blank name → the API's `Name` message shows **next to the field**, not only in the summary
- [ ] Step 11 (stop the API → friendly page)
- [ ] Step 12 (1-minute JWT → "session ended" on the login page)

**Dependencies:** Task 1

**Files likely touched:**
- `src/Klasly.Web/Program.cs`
- `src/Klasly.Web/Infrastructure/BearerTokenHandler.cs`, `ApiExceptionFilter.cs`, `ModelStateExtensions.cs` (new)
- `src/Klasly.Web/Models/Api/TermDtos.cs`, `Repositories/TermRepository.cs` + interface, `Services/TermService.cs` + interface (new)
- `src/Klasly.Web/Areas/Admin/Controllers/HomeController.cs`, `TermsController.cs` (new)
- `src/Klasly.Web/ViewModels/TermFormViewModel.cs` (new)
- `src/Klasly.Web/Areas/Admin/Views/` `_ViewImports`, `_ViewStart`, `Home/Index`, `Terms/Index`, `Terms/Form` (new); `Views/Shared/ApiUnavailable.cshtml` (new); `Views/Shared/_Layout.cshtml`

**Estimated scope:** M–L (the biggest task; it creates the pattern, so it's worth doing slowly)

---

## Task 3: Subjects

**Description:** Copy the Terms pattern for the subject catalog.

**Acceptance criteria:**
- [ ] `Models/Api/SubjectDtos.cs`; repository (list, get, create, update); service; one more client registration.
- [ ] `SubjectsController`: `Index`, `Create` GET/POST, `Edit` GET/POST. No delete (Milestone 4 decision 3).
- [ ] `SubjectFormViewModel` (`Code` [Required, StringLength 20], `Title` [Required, StringLength 150], `Units` [Range 1–10]); `Index` (table sorted by code, as the API returns it) + `Form`.

**Verification:**
- [ ] Build succeeds
- [ ] Step 6 (create; duplicate code → API message, form still filled in)

**Dependencies:** Task 2

**Files likely touched:**
- `src/Klasly.Web/Models/Api/SubjectDtos.cs`, `Repositories/SubjectRepository.cs` + interface, `Services/SubjectService.cs` + interface (new)
- `src/Klasly.Web/Areas/Admin/Controllers/SubjectsController.cs`, `ViewModels/SubjectFormViewModel.cs` (new)
- `src/Klasly.Web/Areas/Admin/Views/Subjects/Index.cshtml`, `Form.cshtml` (new); `Program.cs`

**Estimated scope:** S

---

## Checkpoint A — after Tasks 1–3
- [ ] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [ ] Steps 1–6, 11 and 12 pass
- [ ] Team looks at the layout and form style before more pages copy it

---

## Task 4: Accounts — list + create

**Description:** The admin sees all accounts filtered by role and creates an account showing only the chosen role's fields.

**Acceptance criteria:**
- [ ] `Models/Api/AccountDtos.cs` (`CreateAccountRequest`, `UpdateAccountRequest`, `AccountResponse` with `string Role`); repository (list `?role=`, get, create); service; client registration.
- [ ] `AccountsController.Index(string? role)`: tabs All / Admin / Teacher / Student / Parent; table with name, email, role, ID no. (student number or employee no.), Active/Inactive badge, Edit button; a "New {role}" button for each role.
- [ ] `Create(string role)` GET/POST: an unknown role → `400`/back to the list. `AccountFormViewModel` holds the common fields + every role field with the API's limits (name 150, email 256, password min 8, student number 20, year level 1–6, program 50, employee no. 20, department 100, contact email 256, contact number 20). `Role` is a hidden field; the view shows only that role's block.
- [ ] Required-per-role rules come from the API (`400` detail shown in the summary). The view model doesn't repeat them.

**Verification:**
- [ ] Build succeeds
- [ ] Step 7, first half: create one of each role (each form shows only its fields); student without student number → message; duplicate email → `409` message, form still filled in

**Dependencies:** Task 2

**Files likely touched:**
- `src/Klasly.Web/Models/Api/AccountDtos.cs`, `Repositories/AccountRepository.cs` + interface, `Services/AccountService.cs` + interface (new)
- `src/Klasly.Web/Areas/Admin/Controllers/AccountsController.cs`, `ViewModels/AccountFormViewModel.cs` (new)
- `src/Klasly.Web/Areas/Admin/Views/Accounts/Index.cshtml`, `Form.cshtml` (new); `Program.cs`

**Estimated scope:** M

---

## Task 5: Accounts — edit, deactivate, password reset

**Description:** Edit profile fields, turn an account off and on, and set a new password.

**Acceptance criteria:**
- [ ] Repository update (`PUT`) and deactivate (`DELETE`).
- [ ] `Edit` GET/POST reuses `Form.cshtml`: no role or password fields; an `IsActive` checkbox; an optional "New password" box (blank → `null` → unchanged, min 8 when filled).
- [ ] `Deactivate` POST with `confirm()`. The button is hidden when the account's `userId` equals the signed-in NameIdentifier claim (the API also refuses with `400`).

**Verification:**
- [ ] Build succeeds
- [ ] Step 7, second half: deactivate the new teacher → their login fails; reactivate + reset password → login works with the new password; your own account has no Deactivate button

**Dependencies:** Task 4

**Files likely touched:**
- `src/Klasly.Web/Repositories/AccountRepository.cs` + interface, `Services/AccountService.cs` + interface
- `src/Klasly.Web/Areas/Admin/Controllers/AccountsController.cs`, `ViewModels/AccountFormViewModel.cs`
- `src/Klasly.Web/Areas/Admin/Views/Accounts/Form.cshtml`

**Estimated scope:** S

---

## Task 6: Parent links

**Description:** On a parent's Edit page, list their linked children, link a new one, and unlink.

**Acceptance criteria:**
- [ ] `Models/Api/AccountDtos.cs` gains `LinkStudentRequest`, `LinkedStudentResponse`. Repository: list / link / unlink.
- [ ] `AccountService.GetEditAsync` returns the account plus, for Parents only, the linked students and a dropdown of active students (`GET /api/accounts?role=Student`, by `studentId`) not already linked.
- [ ] `_ParentLinks.cshtml` partial on Edit: table (student no., name, relationship, receives alerts, Unlink button with `confirm()`) + a "Link a student" form (student dropdown, relationship [StringLength 30], receives alerts checkbox, on by default).
- [ ] `LinkStudent` / `UnlinkStudent` POSTs redirect back to Edit with a TempData message. A `409` shows as a TempData error on the Edit page.

**Verification:**
- [ ] Build succeeds
- [ ] Step 8 (link → appears; link again → message; unlink → gone)

**Dependencies:** Task 5

**Files likely touched:**
- `src/Klasly.Web/Models/Api/AccountDtos.cs`, `Repositories/AccountRepository.cs` + interface, `Services/AccountService.cs` + interface
- `src/Klasly.Web/Areas/Admin/Controllers/AccountsController.cs`
- `src/Klasly.Web/Areas/Admin/Views/Accounts/_ParentLinks.cshtml` (new), `Form.cshtml`

**Estimated scope:** S

---

## Checkpoint B — after Tasks 4–6
- [ ] Steps 7 and 8 pass
- [ ] No page ever shows raw JSON or a stack trace

---

## Task 7: Classes — list + create/edit

**Description:** Class list filtered by term; create and edit with subject, teacher and term dropdowns.

**Acceptance criteria:**
- [ ] `Models/Api/ClassDtos.cs`; repository (list `?termId=`, get, create, update); client registration.
- [ ] `ClassService.GetListAsync(int? termId)`: `null` → the active term (all terms if none is active); `0` → all terms. Returns the classes + the term dropdown.
- [ ] `ClassService.GetFormAsync`: subjects, **active** teachers (`?role=Teacher`, value = `teacherId`), terms.
- [ ] `ClassesController`: `Index(int? termId)` (term dropdown with an "All terms" option; table: subject code + title, section, teacher, schedule, room, enrolled count; Edit / Students buttons), `Create` / `Edit` GET/POST. On a failed post, the dropdowns are rebuilt before returning the view.
- [ ] `ClassFormViewModel` (`SubjectId`, `TeacherId`, `TermId` required, `Section` [Required, StringLength 30], `Schedule` [StringLength 100], `Room` [StringLength 30], plus the `SelectList`s).

**Verification:**
- [ ] Build succeeds
- [ ] Step 9 (create with dropdowns; duplicate subject + section + term → message; the term filter changes the list)

**Dependencies:** Tasks 2, 3, 4

**Files likely touched:**
- `src/Klasly.Web/Models/Api/ClassDtos.cs`, `Repositories/ClassRepository.cs` + interface, `Services/ClassService.cs` + interface (new)
- `src/Klasly.Web/Areas/Admin/Controllers/ClassesController.cs`, `ViewModels/ClassFormViewModel.cs` (new)
- `src/Klasly.Web/Areas/Admin/Views/Classes/Index.cshtml`, `Form.cshtml` (new); `Program.cs`

**Estimated scope:** M

---

## Task 8: Class details — enroll, drop

**Description:** One page per class showing who is enrolled, with enroll and drop.

**Acceptance criteria:**
- [ ] `Models/Api/EnrollmentDtos.cs` (`EnrollmentRequest`, `EnrollmentResponse` with `string Status`); `ClassRepository` gains list-enrollments / enroll / drop (enrollments belong to the class page, so no separate repository).
- [ ] `ClassService.GetDetailsAsync(id)`: class + enrollments + a dropdown of active students who are not **Active** in this class (Dropped students can be picked again).
- [ ] `ClassDetailsViewModel`; `Details.cshtml`: class header, enrollment table (student no., name, Active/Dropped badge, Drop button with `confirm()` on Active rows), "Enroll a student" form.
- [ ] `Enroll` POST (`201` new or `200` re-enrolled; both → "Enrolled.") and `Drop` POST redirect back to Details. A `409` shows as a TempData error.

**Verification:**
- [ ] Build succeeds
- [ ] Step 10 (enroll → Active; drop → Dropped; enroll again → Active, same row)

**Dependencies:** Task 7

**Files likely touched:**
- `src/Klasly.Web/Models/Api/EnrollmentDtos.cs` (new), `Repositories/ClassRepository.cs` + interface, `Services/ClassService.cs` + interface
- `src/Klasly.Web/Areas/Admin/Controllers/ClassesController.cs`, `ViewModels/ClassDetailsViewModel.cs` (new)
- `src/Klasly.Web/Areas/Admin/Views/Classes/Details.cshtml` (new)

**Estimated scope:** S–M

---

## Checkpoint C — after Tasks 7–8
- [ ] Steps 9 and 10 pass

---

## Task 9: `/impeccable`, full smoke test, package check, reports, commit

**Description:** Polish, prove everything works together, document it, commit.

**Acceptance criteria:**
- [ ] `/impeccable` run on every view; findings fixed or listed in the report.
- [ ] All 12 smoke steps pass in one run.
- [ ] `dotnet list Klasly.slnx package` → no Microsoft package below 10.x. `Klasly.Web.csproj` has no EF Core package and no `Klasly.Core` / `Klasly.Infrastructure` reference. No secret or connection string in `Klasly.Web`.
- [ ] `docs/reports/<yyyy-MM-dd-HHmm>-milestone-5-web-admin/` with DEVELOPER-DOCS.md, HOW-TO-USE.md (including `dotnet dev-certs https --trust`), FLOWCHARTS.md (login flow + MVC → API sequence; no erDiagram, since there are no database changes). One line in `INDEX.md`.
- [ ] SPEC.md success criteria ticked, status "Done"; tasks ticked in `plan.md` and this file.
- [ ] Committed to `dev`.

**Verification:**
- [ ] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [ ] Every SPEC success criterion ticked

**Dependencies:** Tasks 1–8

**Files likely touched:** views (polish), `docs/reports/…` (new), `docs/reports/INDEX.md`, `SPEC.md`, `tasks/plan.md`, `tasks/todo.md`

**Estimated scope:** S
