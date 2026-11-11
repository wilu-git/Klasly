# Spec: Milestone 13 — Dashboards: Parent portal (MVC) + My attendance / My grades / Child overview (Android)

> Status: **Approved** (2026-11-11). Open questions 1–6 answered as recommended:
> - one shared `StudentOverviewResponse` for students and parents;
> - students get `IsAtRisk = null` (they still see absence rate and weighted average);
> - rate, average and badge come from the latest `RiskFlags` snapshot; counts and lists are live;
> - an unlinked or unknown child → 403 "You can only view your own children";
> - no admin/teacher dashboards in this milestone (failed-emails list → Milestone 14);
> - Android tabs use the Material 3 `NavigationBar` + `rememberSaveable`; no new package.
>
> Plan: `tasks/plan-m13.md` + `tasks/todo-m13.md`.
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.2 "Role dashboards", §27.5 parents see only linked students + at-risk is an early warning, §27.6 RiskFlags "dashboards read the latest", §27.8 risk rules, §11 MVC has no database) → `CLAUDE.md` (§3.3 status codes, §3.4 Parent pages, §3.6 Android screens, §6 Milestone 13) → this spec.
> Milestone 12's spec is in git history (commit `56d0fe5`) and in `docs/reports/2026-11-11-0854-milestone-12-task-5-wire-tick-smoke/`.

## What exists today

- **Data is all there, but nobody can read it as a student or parent.**
  - `AttendanceRecords` (Present / Late / Absent / Excused) from scans, the Worker and teacher corrections.
  - `Assessments` + `Grades` from Milestone 10.
  - `RiskFlags` snapshots from Milestone 11: `AbsenceRate`, `WeightedAverage` (null until a grade is posted), `IsAtRisk`, `ComputedAt`. Written only when a value changes.
- **Web API:**
  - The `StudentOnly` and `ParentOnly` policies exist. `ParentOnly` is not used yet.
  - `api/students/me` has only `POST device` and `PUT fcm-token`.
  - The JWT `sub` is the UserId. `IUserRepository.GetByIdAsync` loads the Student / Guardian profile. `GetLinksAsync(guardianId)` lists a parent's linked students.
  - `LinkedStudentResponse(StudentId, StudentNumber, StudentName, Relationship, ReceivesAlerts)` already exists (admin parent-links page).
- **Web MVC:**
  - `HomeController.Index` sends Admin and Teacher to their areas. Parent and Student see a "Your pages are coming soon" placeholder.
  - Pattern to copy: the `Teacher` area (controller → service → API-backed repository → typed `HttpClient`), `ManilaTime`, `_Alerts`, `ApiExceptionFilter`.
- **Android:**
  - `MainActivity` picks a screen by role: Student → `ScanScreen` only; Parent → `ParentHomeScreen`, a placeholder that says "Your child overview comes in a later update".
  - No navigation library. Material 3 from Compose BOM 2026.02.01, which includes `NavigationBar` and `PullToRefreshBox`.
  - Manila time formatting is in `ScanViewModel`. `ApiText` + `ApiTextTest` cover API error text.

## Assumptions (correct these before we build)

1. **One read model for both roles.** A "student overview" covers one student in the **active term**, with one entry per **Active** enrollment (dropped classes are hidden). Each entry has:
   - class: subject code + title, section, teacher name;
   - attendance: counts of Present / Late / Absent / Excused, plus every record, newest first (session start time + status);
   - grades: every assessment of the class (title, type, max score, weight) with the student's score, or null = "not graded yet";
   - risk: `AbsenceRate`, `WeightedAverage`, `IsAtRisk`, `ComputedAt` from the **latest** RiskFlags snapshot (see Q3). All null if the Worker hasn't run yet.
2. **Read-only.** No new tables, no migration, no new package, no writes. Parents cannot change alert settings (the admin does that).
3. **Endpoints (REST, under `/api/`):**
   - `GET /api/students/me/overview`: `StudentOnly`. The logged-in student's overview.
   - `GET /api/parent/children`: `ParentOnly`. The linked students (`List<LinkedStudentResponse>`, reused), ordered by name.
   - `GET /api/parent/children/{studentId}/overview`: `ParentOnly`. Same response shape. A student not linked to this parent → **403** "You can only view your own children" (Q4).
   - No active term → `200` with an empty `Classes` list and `TermName = null`. The clients show "No classes this term".
4. **Ownership comes from the token, never from the client.** The student endpoint has no id in the URL. The parent endpoints look up the guardian from `sub`, then check the `StudentGuardians` link. An inactive parent/student user can't log in, so it needs no extra check here.
5. **Times are UTC in JSON.** MVC shows them with `ManilaTime`; Android with `ZoneId.of("Asia/Manila")` (CLAUDE.md §3.1).
6. **MVC Parent area** (Bootstrap, same look as the Teacher area):
   - `HomeController` sends Parent → `Parent/Children/Index`. The nav shows "My children".
   - **Index:** linked children (name, student number). Exactly one child → redirect straight to that child's page. None → "No children are linked to your account yet. Ask the school admin."
   - **Details (child overview):** one card per class with:
     - attendance counts;
     - absence rate;
     - weighted average (or "No grades yet");
     - an **At risk** / **On track** badge, with the line "This is an early-warning indicator for review, not a final grade" (§27.5) and "Updated <Manila time>";
     - two tables you can expand: attendance records (date, time, status) and grades (assessment, score / max, weight).
   - Student users who log into MVC see "Use the Klasly app on your phone to scan and see your records" instead of "coming soon". No student pages on the web.
7. **Android, Student:** a bottom `NavigationBar` with three tabs: **Scan** (today's screen, unchanged), **Attendance**, **Grades**. The selected tab is kept with `rememberSaveable`; no navigation library (Q6).
   - **Attendance tab:** per class, the counts, the absence rate and the list of records (date, time in Manila, status).
   - **Grades tab:** per class, each assessment with score / max and weight, plus the weighted average ("No grades yet" when null).
   - Both tabs share one `StudentOverviewViewModel`, which loads `students/me/overview` once. Pull-to-refresh (`PullToRefreshBox`) reloads it.
8. **Android, Parent:** `ParentHomeScreen` becomes the **Child overview**.
   - It loads `parent/children`. One child → shows the overview directly. Several → a row of filter chips to pick the child.
   - It shows the same per-class cards as the MVC Details page (attendance summary, weighted average, at-risk badge + the early-warning line), with grades and records you can expand.
   - Pull-to-refresh. Log out stays.
9. **Every Android screen** shows loading, empty and error states. Error text comes from `ApiText`, same as scan; a 401 sends the user back to Login as today.
10. **Thresholds aren't sent.** Clients show `IsAtRisk` as computed by the Worker; they never re-apply the 20% / 75 rule.

## Objective

Parents and students can finally see the data Klasly collects. A parent opens the web portal or the app and sees, for each linked child and each class: attendance, grades, and whether the child is flagged at-risk. A student sees their own attendance and grades in the app. The Web API decides who may see what (§27.5); the clients only display.

### User stories

- As a **parent**, I see each of my children's attendance, grades and at-risk status per class, on the web or on my phone, so I know where to help.
- As a **parent**, I can never see a child who isn't linked to me, even if I change the id in the URL.
- As a **student**, I see my attendance records and my grades per class in the app, right after I scan.

## Design

```
Android (Student)  GET /api/students/me/overview ──┐
Android / MVC (Parent)                             │
  GET /api/parent/children                         ├─► StudentsController / ParentController
  GET /api/parent/children/{id}/overview ──────────┘        │
                                                    OverviewService (API)
                                                      • student: userId → StudentId
                                                      • parent:  userId → GuardianId → link check (403)
                                                            │
                                                    IOverviewRepository.GetAsync(studentId)   (Infrastructure)
                                                      one projection query: active term → Active enrollments →
                                                      class, records, assessments + this student's grades,
                                                      latest RiskFlags row
                                                            │
                                                    StudentOverviewResponse (Core DTO)
```

```csharp
public record StudentOverviewResponse(int StudentId, string StudentNumber, string FullName, string? TermName, List<ClassOverview> Classes);
public record ClassOverview(int ClassId, string SubjectCode, string SubjectTitle, string Section, string TeacherName,
    int Present, int Late, int Absent, int Excused,
    decimal? AbsenceRate, decimal? WeightedAverage, bool? IsAtRisk, DateTime? RiskComputedAtUtc,
    List<AttendanceItem> Attendance, List<GradeItem> Grades);
public record AttendanceItem(int SessionId, DateTime StartTimeUtc, string Status);
public record GradeItem(int AssessmentId, string Title, string Type, decimal MaxScore, decimal WeightPercent, decimal? Score, DateTime? PostedAtUtc);
```

## Commands

```
Build:    dotnet build Klasly.slnx
Test:     dotnet test --solution Klasly.slnx
API:      dotnet run --project src/Klasly.API          (Swagger UI at /swagger in Development)
Web:      dotnet run --project src/Klasly.Web
Worker:   dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"   (so RiskFlags exist)
Android:  cd android/KlaslyApp && ./gradlew assembleDebug testDebugUnitTest
Model:    dotnet ef migrations has-pending-model-changes -p src/Klasly.Infrastructure -s src/Klasly.API   (must say none)
Packages: dotnet list Klasly.slnx package   (Microsoft.* all 10.x, nothing new)
```

## Project structure (files this milestone touches)

| Path | New / changed | Why |
| --- | --- | --- |
| `src/Klasly.Core/DTOs/StudentOverviewResponse.cs` | new | The four records above. |
| `src/Klasly.Core/Interfaces/IOverviewRepository.cs` | new | `GetAsync(studentId, ct)` → overview or null (unknown student). |
| `src/Klasly.Infrastructure/Repositories/OverviewRepository.cs` | new | The single projection query (no tracking). |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | changed | Register the repository. |
| `src/Klasly.API/Services/OverviewService.cs` + `Interfaces/IOverviewService.cs` | new | Who-may-see-what + calls the repository. |
| `src/Klasly.API/Controllers/StudentsController.cs` | changed | `GET overview`. |
| `src/Klasly.API/Controllers/ParentController.cs` | new | `GET children`, `GET children/{studentId}/overview`. |
| `src/Klasly.API/Program.cs` | changed | Register the service. |
| `tests/Klasly.Tests/OverviewTests.cs` | new | LocalDB: what's included, risk, ownership. |
| `src/Klasly.Web/Models/Api/OverviewDtos.cs` | new | Web copies of the response records (Web references no Core). |
| `src/Klasly.Web/Repositories/ParentRepository.cs` + interface | new | Typed `HttpClient` calls. |
| `src/Klasly.Web/Services/ParentService.cs` + interface | new | Builds the view models. |
| `src/Klasly.Web/ViewModels/ChildOverviewViewModel.cs` | new | Overview + Manila-time labels + sibling list. |
| `src/Klasly.Web/Areas/Parent/Controllers/ChildrenController.cs` | new | Index, Details. `[Authorize(Roles = "Parent")]`. |
| `src/Klasly.Web/Areas/Parent/Views/…` | new | `Children/Index`, `Children/Details`, `_ViewImports`, `_ViewStart`. |
| `src/Klasly.Web/Controllers/HomeController.cs`, `Views/Shared/_Layout.cshtml`, `Views/Home/Index.cshtml`, `Program.cs` | changed | Parent redirect, nav item, student message, DI. |
| `android/…/data/Dtos.kt`, `KlaslyApi.kt`, `KlaslyRepository.kt` | changed | 3 calls + DTOs. |
| `android/…/data/DisplayText.kt` + `test/…/DisplayTextTest.kt` | new | Pure formatting: percent, average / "No grades yet", score / max, Manila date-time. Unit-tested. |
| `android/…/ui/student/StudentHome.kt` | new | Bottom bar with the three tabs. |
| `android/…/ui/student/StudentOverviewViewModel.kt`, `AttendanceTab.kt`, `GradesTab.kt` | new | The two new tabs. |
| `android/…/ui/overview/ClassCards.kt` | new | Shared per-class cards (used by student tabs and the parent screen). |
| `android/…/ui/parent/ParentHomeScreen.kt` + `ParentViewModel.kt` | changed / new | Child overview. |
| `android/…/MainActivity.kt` | changed | Student → `StudentHome`, Parent → view model. |

## Code style

Same as earlier milestones: primary constructors, `CancellationToken` everywhere, DTOs only, problem-details errors via the existing exceptions (`ForbiddenException` → 403). In the service, the ownership check is one line before the read:

```csharp
if (!await users.IsLinkedAsync(guardianId, studentId, ct)) throw new ForbiddenException("You can only view your own children");
```

(Uses the existing `GetLinkAsync` if it fits; no new method unless needed.) Android: `ViewModel` + `StateFlow<UiState>` (`Loading | Empty | Error(message) | Ready(data)`), same as `ScanViewModel`.

## Testing strategy

- **`OverviewTests`** (LocalDB, xUnit, the existing `DatabaseFixture`):
  - only active-term classes and Active enrollments are listed; a Dropped class is hidden;
  - counts per status match the records; records are newest first;
  - every assessment is listed, with Score null when ungraded;
  - risk = the **latest** snapshot, or all null without one;
  - no active term → empty list;
  - a parent reading a linked child → 200; an unlinked child → `ForbiddenException`; an unknown id → 403 too (no hint whether it exists);
  - student risk fields follow Q2.
- **Role access:** the endpoints use the existing policies (Teacher / Admin on these routes → 403; no token → 401). Checked in the smoke test; full role-access tests are Milestone 14.
- **Android:** `DisplayTextTest` (JVM): 0.2 → "20%", null average → "No grades yet", 87.5/100 → "87.5 / 100", a UTC instant → Manila date/time. Then `./gradlew assembleDebug testDebugUnitTest`.
- **Smoke (manual):**
  - Run API + Web + Worker.
  - Log in on the web as a seeded parent: see the children, a child's classes, badge and tables. Change the URL to another student's id → the API's 403 message.
  - On the emulator, as a student: the Attendance and Grades tabs show the seeded data; pull to refresh.
  - As a parent on the emulator: the child overview, and switching between children.
- `/impeccable` on the new Razor views and Compose screens before calling the UI done.

## Boundaries

- **Always:**
  - authorization in the API (the link check), never only in the client;
  - UTC in, Manila only on screen;
  - show the early-warning line next to any at-risk badge;
  - loading / empty / error states;
  - build + tests + `assembleDebug`;
  - reports.
- **Ask first:**
  - any schema change or new package (e.g. navigation-compose);
  - a risk trend chart (Nice-to-Have);
  - export to PDF/Excel (Nice-to-Have);
  - excuse requests (Nice-to-Have);
  - letting parents change alert settings;
  - admin or teacher dashboards (Q5).
- **Never:**
  - return EF entities;
  - accept a student id from a student client;
  - recompute risk in the clients;
  - show another family's child;
  - real people's data in seeds or screenshots (RA 10173).

## Success criteria

- [x] `GET /api/students/me/overview` returns the logged-in student's active-term classes with attendance counts + records, all assessments with scores (null = ungraded), and the latest risk values (per Q2).
- [x] `GET /api/parent/children` lists only the parent's linked students. `GET /api/parent/children/{id}/overview` returns the same shape for a linked child and 403 for any other id.
- [x] MVC: a parent lands on My children (or straight on the only child), sees per-class attendance, grades and an At risk / On track badge with the early-warning line, times in Manila. API errors are shown with `_Alerts`.
- [x] Android, Student: Scan / Attendance / Grades tabs. Attendance and Grades show the API data, with loading, empty and error states and pull-to-refresh. *(Checked on the Pixel_7 emulator, 2026-11-11.)*
- [x] Android, Parent: the child overview replaces the placeholder, with a child picker when there are several. *(Checked on the Pixel_7 emulator, 2026-11-11.)*
- [x] No migration, no new package, Microsoft.* all 10.x; `dotnet build`, `dotnet test`, `./gradlew assembleDebug testDebugUnitTest` pass.
- [x] Reports written; INDEX line added.

## Open questions (my recommendation first)

1. **One shared response for students and parents?** **Recommend yes.** One repository query, one DTO, one set of tests; the Android class cards are reused by both roles. Alternative: separate `attendance` and `grades` endpoints for students (two queries, two DTOs, and the parent still needs both).
2. **Do students see their own at-risk flag?** §27.5 calls it "an early-warning indicator for teachers to review"; CLAUDE.md lists at-risk only on the Parent screens. **Recommend: no.** The student response sends `IsAtRisk = null`. Students still see their absence rate and weighted average, which they can work out from their own records anyway. Alternative: show it to students too, with the same early-warning line.
3. **Where do absence rate and weighted average come from?** **Recommend the latest `RiskFlags` snapshot** (§27.6 "dashboards read the latest"). One source of truth with the Worker's rules, and the badge and the numbers always agree. The cost: up to one tick (5 min) behind a fresh scan or grade, and "—" until the Worker's first run. The counts and lists are always live. Alternative: compute live in the API, which copies the Worker's formula into a second place.
4. **Unlinked child: 403 or 404?** **Recommend 403** "You can only view your own children", as CLAUDE.md §3.3 says ("403 not owner"). An unknown id gets the same 403, so it doesn't reveal which ids exist. Alternative: 404 for both.
5. **Admin/teacher dashboards are not in this milestone.** That includes the "Failed emails" list that §27.8 puts on the admin dashboard (deferred from Milestone 12). CLAUDE.md §6 scopes Milestone 13 to the Parent portal and the Android screens. **Recommend: leave them out**, and add the failed-emails table to Milestone 14 hardening (or as a small extra task here if you want it now).
6. **Android student navigation.** **Recommend a Material 3 `NavigationBar` with tab state in `rememberSaveable`.** It's already in the installed Material 3, so no new dependency, and three tabs don't need a nav graph. Alternative: add `androidx.navigation:navigation-compose` (a new package, more setup, and back-stack handling we don't need).
