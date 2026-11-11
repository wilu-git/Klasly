# Implementation Plan: Milestone 13 — Dashboards: Parent portal (MVC) + My attendance / My grades / Child overview (Android)

> Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-11). Task details and acceptance criteria are in [`todo-m13.md`](./todo-m13.md). Milestone 12 is in `plan-m12.md` / `todo-m12.md` (complete except Task 6, the team's Brevo setup, which doesn't block this).

## Context

Milestones 1–12 collect attendance, grades and risk flags, but no student or parent can read them:
- the API has no read endpoints for those roles;
- MVC sends parents to a "coming soon" page;
- the Android parent screen is a placeholder and students only have Scan.

Milestone 13 adds one read-only "student overview" in the API and shows it in the MVC Parent portal and on Android (student tabs + parent Child overview). There is no migration and no new package.

**Decisions (spec open questions, 2026-11-11):**
- Q1: one shared `StudentOverviewResponse` for students and parents.
- Q2: students get `IsAtRisk = null`; they still see the absence rate and weighted average.
- Q3: rate, average and badge come from the latest `RiskFlags` snapshot; counts and lists are live.
- Q4: an unlinked or unknown child → 403 "You can only view your own children".
- Q5: no admin or teacher dashboards; the failed-emails list moves to Milestone 14.
- Q6: Android tabs use the Material 3 `NavigationBar` + `rememberSaveable`; no navigation library.

## Reuse (don't rebuild)

- **API:**
  - `ForbiddenException` → 403 through `ApiExceptionHandler`;
  - `IUserRepository.GetByIdAsync` (loads the Student / Guardian profile), `GetLinksAsync(guardianId)` (ordered by name), `GetLinkAsync(studentId, guardianId)` (the ownership check);
  - `LinkedStudentResponse`, reused as is for `GET api/parent/children`;
  - the `CurrentUserId` pattern in `StudentsController`;
  - the `StudentOnly` / `ParentOnly` policies (`src/Klasly.API/Program.cs:88-91`).
- **Repository returning a DTO:** `RiskRepository.ListInputsAsync` → `RiskInput`.
- **Tests:**
  - `DatabaseFixture`, `TestData.NewClassAsync` (term 1 is active; Rosa = teacher 1);
  - the seed links guardian 1 (user 10) → students 1 and 2, and guardian 2 → student 3.
- **Web:**
  - the `Teacher` area pattern (`[Area("Teacher"), Authorize(Roles = "Teacher")]`);
  - the `AddHttpClient<TRepository, TImplementation>` helper + `BearerTokenHandler` (`src/Klasly.Web/Program.cs:50`);
  - `http.GetApiAsync<T>`;
  - `ManilaTime.Format` / `FormatTime`, `_Alerts`, `ApiExceptionFilter`;
  - nav items in `_Layout.cshtml`.
- **Android:**
  - `KlaslyRepository.call {}` (`ApiResult`, 401 → logout);
  - `parseUtc` / `errorMessage` in `data/ApiText.kt`;
  - the `StateFlow` + sealed UiState pattern of `ScanViewModel`;
  - `ZoneId.of("Asia/Manila")`;
  - `ApiTextTest` as the JVM test model;
  - Material 3 `NavigationBar`, `FilterChip`, `PullToRefreshBox` (Compose BOM 2026.02.01).

## Design notes

- **`OverviewRepository.GetAsync(studentId, ct)`** (`Klasly.Infrastructure`, no tracking). Returns null when the student doesn't exist.
  - `Enrollments.Where(e => e.StudentId == id && e.Status == Active && e.Class.Term.IsActive)`. Each row maps to a `ClassOverview` with:
    - subject code and title, section, teacher `User.FullName`;
    - per-status counts plus records from `AttendanceRecords.Where(r => r.StudentId == id && r.Session.ClassId == e.ClassId)`, ordered by `Session.StartTime` desc;
    - `e.Class.Assessments` (ordered by `DatePosted`, then title) with this student's Score / PostedAt, or null;
    - latest risk from `e.RiskFlags.OrderByDescending(ComputedAt).ThenByDescending(RiskFlagId).FirstOrDefault()`.
  - `TermName` from the active term. Classes ordered by subject code, then section.
  - Use `AsSplitQuery` if EF warns. Fallback if it won't translate: three small queries stitched together in memory; the report says which shape was used.
- **`OverviewService`** (API):
  - `GetForStudentAsync(userId)` → StudentId → repository, then `IsAtRisk = null` on every class (Q2).
  - `ListChildrenAsync(parentUserId)` → guardian → `GetLinksAsync` → `LinkedStudentResponse`.
  - `GetForParentAsync(parentUserId, studentId)`: no link → `ForbiddenException("You can only view your own children")`; otherwise the repository.
- **Controllers:**
  - `StudentsController` gains `GET api/students/me/overview`.
  - New `ParentController`: `[Route("api/parent/children")]`, `ParentOnly`, with `GET` and `GET {studentId:int}/overview`.
- **Web:**
  - `Models/Api/OverviewDtos.cs` (copies; Web references no Core).
  - `ParentRepository` / `ParentService`.
  - `ChildOverviewViewModel(Overview, Children)` for switching between siblings.
  - `Areas/Parent/Controllers/ChildrenController`: Index redirects to Details when there is exactly one child. Details has one card per class: counts, rate, average or "No grades yet", an At risk / On track / Not computed yet badge, the early-warning line, and "Updated …" in Manila time. Tables sit in `<details>`.
- **Android:**
  - `DisplayText.kt`: pure formatting functions, unit-tested.
  - `ClassCards.kt`: shared by both roles. The risk badge shows only when `isAtRisk != null`.
  - `OverviewUiState = Loading | Empty | Error(msg) | Ready(data)` + `refresh()` in `StudentOverviewViewModel` and `ParentViewModel`. The parent model also holds `children` + `selectedId`.
  - `MainActivity`: Student → `StudentHome` (Scan / Attendance / Grades). Parent → `ParentHomeScreen(vm)`. View models are keyed by token.

## Task list

### Phase 1: Web API
- [x] Task 1: overview DTOs + repository query + repository tests (M)
- [x] Task 2: `OverviewService` + student and parent endpoints + service tests (S)

### Checkpoint A
- [x] Build and tests pass, no pending model changes; Swagger smoke of the three endpoints, including 403s

### Phase 2: Web MVC Parent portal
- [x] Task 3: Web DTOs, `ParentRepository`, `ParentService`, view model, DI (S)
- [x] Task 4: Parent area (Index, Details), Home redirect, nav, student message (M)
- [ ] Task 5: `/impeccable` on the parent views + browser smoke (S)

### Phase 3: Android
- [ ] Task 6: DTOs, API calls, repository, `DisplayText` + tests (S)
- [ ] Task 7: student tabs (shared class cards, Attendance, Grades, NavigationBar) (M)
- [ ] Task 8: parent Child overview (view model, child chips) (M)
- [ ] Task 9: `/impeccable` on the Compose screens + emulator smoke (S)

### Checkpoint B
- [ ] Every spec success criterion met; nothing new in `dotnet list package`; Microsoft.* 10.x

## Risks and mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| EF can't translate the single projection (nested lists + latest risk) | Med | `AsSplitQuery`, or the 3-query fallback; Task 1's tests prove the result either way. |
| A "no active term" test would deactivate term 1 under parallel tests | Low | Not tested directly. The inactive-term-class test covers the same filter; the empty case is checked in the smoke. |
| Risk numbers lag a fresh scan or grade by up to one tick | Low | Expected (Q3). The UI shows "Updated <time>". |
| The area name `Parent` clashes with a type or namespace | Low | Namespace `Klasly.Web.Areas.Parent.Controllers`; rename only if the compiler complains. |

## Out of scope

- Admin and teacher dashboards; the failed-emails list (Milestone 14).
- Risk trend chart, exports, excuse requests (Nice-to-Have).
- Parents editing alert settings; FCM tap-to-open-Grades; navigation-compose.

## Open questions

None for building.
