# Tasks: Milestone 13 — Dashboards: Parent portal (MVC) + My attendance / My grades / Child overview (Android)

> From [`plan-m13.md`](./plan-m13.md) and [`SPEC.md`](../SPEC.md). Every task ends with `dotnet build Klasly.slnx` + `dotnet test --solution Klasly.slnx` (Android tasks also `cd android/KlaslyApp && ./gradlew assembleDebug testDebugUnitTest`), one commit, and the three reports + an INDEX line (CLAUDE.md §5).

## Task 0: Housekeeping
- [x] SPEC.md marked Approved with the answers; `plan-m13.md` + this file written.

## Task 1: Overview query (test first)

**What:**
- `src/Klasly.Core/DTOs/StudentOverviewResponse.cs`: `StudentOverviewResponse`, `ClassOverview`, `AttendanceItem`, `GradeItem` (spec "Design");
- `src/Klasly.Core/Interfaces/IOverviewRepository.cs`: `GetAsync(studentId, ct)` → overview or null;
- `src/Klasly.Infrastructure/Repositories/OverviewRepository.cs` + registration in `DependencyInjection.cs`.

**Acceptance:**
- [x] Only Active enrollments in active-term classes are listed. A Dropped enrollment and a class in an inactive term are hidden.
- [x] Present / Late / Absent / Excused counts match the student's records in that class. Records are newest first (by session start), with status as a string.
- [x] Every assessment of the class is listed. An ungraded one has `Score = null` and `PostedAtUtc = null`. Another student's grade never shows.
- [x] Risk fields = the latest RiskFlags row (newest `ComputedAt`, ties by id). No row → all four null.
- [x] `TermName` = the active term's name. An unknown student id → null.

**Verify:** `dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.OverviewTests`

**Files:** the three new files above, `src/Klasly.Infrastructure/DependencyInjection.cs`, `tests/Klasly.Tests/OverviewTests.cs`. **Depends on:** none.

## Task 2: Service + endpoints (test first)

**What:**
- `src/Klasly.API/Services/OverviewService.cs` + `Interfaces/IOverviewService.cs`;
- `GET api/students/me/overview` in `StudentsController`;
- new `src/Klasly.API/Controllers/ParentController.cs` (`GET api/parent/children`, `GET api/parent/children/{studentId}/overview`, `ParentOnly`);
- registration in `Program.cs`.

**Acceptance:**
- [x] A student's overview has `IsAtRisk = null` on every class, while `AbsenceRate` / `WeightedAverage` are kept.
- [x] Parent user 10 → children = students 1 and 2 (by name), and the overview of student 1 → data.
- [x] Parent user 10 → student 3 or an unknown id → `ForbiddenException` "You can only view your own children".

**Verify:** `dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.OverviewTests`

**Files:** the service + interface, `StudentsController.cs`, `ParentController.cs`, `src/Klasly.API/Program.cs`, `tests/Klasly.Tests/OverviewTests.cs`. **Depends on:** Task 1.

## Checkpoint A
- [x] Build clean, all tests pass, `dotnet ef migrations has-pending-model-changes …` says none.
- [x] Swagger smoke:
  - student → overview;
  - parent user 10 → children [1, 2];
  - parent → student 3 → 403;
  - teacher token on the parent routes → 403;
  - no token → 401.

## Task 3: Web data layer

**What:**
- `src/Klasly.Web/Models/Api/OverviewDtos.cs`;
- `Repositories/ParentRepository.cs` + `Interfaces/IParentRepository.cs`;
- `Services/ParentService.cs` + `Interfaces/IParentService.cs`;
- `ViewModels/ChildOverviewViewModel.cs`;
- registrations in `Program.cs` (typed client with `BearerTokenHandler`, like the others).

**Acceptance:**
- [x] `ParentService` returns the children list, and a view model with the overview + the children (for the sibling switcher).
- [x] API errors pass through as `ApiException`, so the existing filter shows them.

**Verify:** `dotnet build Klasly.slnx`

**Files:** the files above. **Depends on:** Task 2.

## Task 4: Parent area pages

**What:**
- `src/Klasly.Web/Areas/Parent/Controllers/ChildrenController.cs` (`[Area("Parent"), Authorize(Roles = "Parent")]`, Index + Details);
- `Areas/Parent/Views/Children/Index.cshtml`, `Details.cshtml`, `_ViewImports.cshtml`, `_ViewStart.cshtml`;
- `HomeController` (Parent → Children/Index), `_Layout.cshtml` ("My children" nav item), `Views/Home/Index.cshtml` (student message).

**Acceptance:**
- [x] Index lists the children (name + student number). Exactly one child → redirect to Details. None → "No children are linked to your account yet. Ask the school admin."
- [x] Details: per class, the counts, absence rate, weighted average or "No grades yet", and an At risk / On track / Not computed yet badge with "This is an early-warning indicator for review, not a final grade" and "Updated <Manila time>". Expandable attendance (date, time, status) and grades (assessment, score / max, weight) tables. A switcher appears when there are several children.
- [x] No active term → "No classes this term".
- [x] Another family's id in the URL → the API's 403 message is shown (not a crash).
- [x] A student logging into MVC sees "Use the Klasly app on your phone to scan and see your records".

**Verify:** `dotnet build Klasly.slnx`; run API + Web and open the pages as parent user 10.

**Files:** the files above. **Depends on:** Task 3.

## Task 5: Parent portal polish
- [x] `/impeccable` on `Children/Index` and `Details`; fixes applied.
- [x] Browser smoke (one-child redirect not exercised live: needs a temporary unlink; see the Task 5 report): parent user 10 (two children, switcher), a one-child parent → redirect (every seeded parent has two children: unlink one in Admin › Accounts for the check, then link it back), times in Manila, keyboard and screen-reader labels on the badge and tables.

**Depends on:** Task 4.

## Task 6: Android data layer (test first)

**What:**
- `data/Dtos.kt`: `StudentOverview`, `ClassOverview`, `AttendanceItem`, `GradeItem`, `LinkedStudent`;
- `data/KlaslyApi.kt`: `GET students/me/overview`, `GET parent/children`, `GET parent/children/{id}/overview`;
- `data/KlaslyRepository.kt`: three `call {}` wrappers;
- `data/DisplayText.kt` + `test/.../data/DisplayTextTest.kt`.

**Acceptance:**
- [x] `percent(0.2) = "20%"`, `percent(null) = "—"`, `average(null) = "No grades yet"`, `average(87.5) = "87.5"`, `score(87.5, 100) = "87.5 / 100"`, `score(null, 100) = "Not graded yet"`.
- [x] `manilaDateTime("2026-11-10T01:00:00Z")` = "Nov 10, 9:00 AM".
- [x] The DTOs parse a sample JSON body (nullable fields included).

**Verify:** `./gradlew assembleDebug testDebugUnitTest`

**Files:** the files above. **Depends on:** Task 2 (contract).

## Task 7: Student tabs

**What:**
- `ui/overview/ClassCards.kt` (`AttendanceCard`, `GradesCard`, optional risk badge);
- `ui/student/StudentOverviewViewModel.kt`, `AttendanceTab.kt`, `GradesTab.kt`, `StudentHome.kt`;
- `MainActivity.kt` (Student → `StudentHome`).

**Acceptance:**
- [x] A bottom bar with Scan / Attendance / Grades. The tab survives rotation. Scan works exactly as before.
- [x] Attendance and Grades show loading, empty ("No classes this term") and error (API text) states, plus a Ready list per class.
- [x] Pull-to-refresh reloads. A 401 returns to Login with the message.
- [x] No risk badge on student screens (`isAtRisk` is null).

**Verify:** `./gradlew assembleDebug testDebugUnitTest`; emulator as student 1.

**Files:** the files above. **Depends on:** Task 6.

## Task 8: Parent Child overview

**What:** `ui/parent/ParentViewModel.kt`; rewrite `ui/parent/ParentHomeScreen.kt`; `MainActivity.kt` (Parent → view model).

**Acceptance:**
- [x] Loads the children. None → "No children are linked to your account yet." One → that child's overview directly. Several → FilterChips that switch the overview.
- [x] Per-class cards with the attendance summary, weighted average, At risk / On track / Not computed yet badge + the early-warning line, and expandable grades and records.
- [x] Loading, empty, error and pull-to-refresh, as for students. Log out kept.

**Verify:** `./gradlew assembleDebug testDebugUnitTest`; emulator as parent user 10.

**Files:** the files above. **Depends on:** Task 7 (shared `ClassCards`).

## Task 9: Android polish
- [x] `/impeccable` on `StudentHome`, the tabs and `ParentHomeScreen`: static code review against the craft floor (touch targets, headings, TalkBack labels, contrast); no changes needed. A visual pass on a device is still open with the smoke below.
- [ ] **Open (team):** Emulator smoke (the headless Pixel_7 emulator was too unstable to drive from this session; see the Task 9 report): student 1 (tabs match the web data, refresh), parent user 10 (chips), TalkBack labels on the badges, touch targets ≥ 48 dp.

**Depends on:** Task 8.

## Checkpoint B
- [x] Every spec success criterion ticked in `SPEC.md`, except the two Android ones waiting on the device check.
- [x] `dotnet list Klasly.slnx package` shows nothing new and Microsoft.* at 10.x. No migration.
