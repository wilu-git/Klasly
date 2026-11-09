# Milestone 7 — Tasks (Web MVC Teacher session page)

> Plan: [`plan.md`](./plan.md) · Spec: [`SPEC.md`](../SPEC.md). Web MVC only: no API or database change.
> Build after every task: `dotnet build Klasly.slnx`. There is no test project (spec assumption 8), so each task is checked by hand in the browser with the API (`https://localhost:7256`) and the website (`https://localhost:7144`) running.
> Seed logins: `rosa.villanueva@klasly.test` (ELECT3), `mark.delossantos@klasly.test`, plus the seed admin, parent and students.

---

## Task 1: Teacher area + My classes page

**Description:** Create the `Teacher` area, built the same way as the Admin area. A teacher who opens `/` goes to **My classes**, a table of their own classes from `GET api/teacher/classes` (active term first, in the order the API sends). The navbar gets a "My classes" link for teachers. The welcome page shows only for Parents.

**Acceptance criteria:**
- [x] `/Teacher/Classes` lists only the logged-in teacher's classes with subject code + title, section, term, schedule, room, enrolled count and a "Sessions" button (links to `Classes/Details/{id}`).
- [x] `/` sends a Teacher to `/Teacher/Classes` and an Admin to `/Admin`. A Parent still sees the welcome page, with text for Parents only.
- [x] An Admin or a Parent who opens `/Teacher/Classes` gets Access denied (`[Authorize(Roles = "Teacher")]`).

**Verification:**
- [x] Build succeeds: `dotnet build Klasly.slnx`
- [x] Manual check: smoke step 1 (except the "Teacher B's session URL" part, which comes in Task 3)

**Dependencies:** None

**Files likely touched:**
- `src/Klasly.Web/Repositories/Interfaces/ISessionRepository.cs`, `Repositories/SessionRepository.cs` (new: `ListMyClassesAsync` only for now)
- `src/Klasly.Web/Services/Interfaces/ISessionService.cs`, `Services/SessionService.cs` (new)
- `src/Klasly.Web/Areas/Teacher/Controllers/ClassesController.cs`, `Areas/Teacher/Views/_ViewImports.cshtml`, `_ViewStart.cshtml`, `Classes/Index.cshtml` (new)
- `src/Klasly.Web/Program.cs`, `Controllers/HomeController.cs`, `Views/Shared/_Layout.cshtml`, `Views/Home/Index.cshtml` (modified)

**Estimated scope:** Medium. There are more than 5 files, but `_ViewImports`/`_ViewStart` are copies of Admin's and the changes to existing files are one line each.

---

## Task 2: Class details with the session list (Manila times)

**Description:** `Classes/Details/{id}` shows the class header and the class's sessions, newest first: date, start–end in Manila time, a status badge and a link. The header comes from the teacher's class list (a class not in it → 404). Add the session DTOs and the `ManilaTime` helper.

**Acceptance criteria:**
- [ ] The sessions created in Milestone 6 appear newest first, with Manila times such as "Nov 10, 9:00 AM – 10:30 AM" and a badge for each status (Scheduled / Open / Closed / Finalized / Cancelled).
- [ ] Another teacher's class id → 404 or Access denied (from `GET api/classes/{id}/sessions` → 403). It never shows that class's data.
- [ ] `ManilaTime.ToLocal` and `ToOffset` round-trip: 01:00 UTC ↔ 09:00 Manila (+08:00).

**Verification:**
- [ ] Build succeeds: `dotnet build Klasly.slnx`
- [ ] Manual check: smoke step 2. Compare one session's time with the UTC value from Swagger (`GET /api/sessions/{id}`).

**Dependencies:** Task 1

**Files likely touched:**
- `src/Klasly.Web/Models/Api/SessionDtos.cs` (new: `CreateSessionRequest`, `SessionResponse`, `QrTokenResponse`, `SessionAttendanceRow`, with string statuses)
- `src/Klasly.Web/Infrastructure/ManilaTime.cs` (new)
- `src/Klasly.Web/ViewModels/TeacherClassViewModel.cs` (new)
- `SessionRepository` / `SessionService` / `ClassesController` (modified), `Areas/Teacher/Views/Classes/Details.cshtml` (new)

**Estimated scope:** Medium (4–5 files)

### Checkpoint A: after Tasks 1–2
- [ ] `dotnet build Klasly.slnx`: 0 errors, no new warnings
- [ ] Smoke steps 1–2 pass
- [ ] Review with the team before continuing

---

## Task 3: Start session (create + open) → session page

**Description:** Add a **Start session** form on the class details page, prefilled with start = now, end = now + 90 min (Manila time) and Late after 15 min. Posting it calls `POST api/classes/{id}/sessions` and then `POST api/sessions/{id}/open`, and redirects to `Sessions/Details/{id}`. For now the session page shows the header (class, Manila times, status badge, Late minutes). The QR comes in Task 5.

**Acceptance criteria:**
- [ ] With the default values, one click opens a session and lands on its page with status **Open**.
- [ ] End before start, or an end in the past → the API's message on the form, and the entered values are kept (`ModelState.AddApiErrors`).
- [ ] If the create works but the open fails, the page redirects to the session with a TempData error, and the session stays Scheduled (plan decision 6).
- [ ] Teacher B opening Teacher A's session URL → Access denied.

**Verification:**
- [ ] Build succeeds: `dotnet build Klasly.slnx`
- [ ] Manual check: smoke step 3 (without the QR) and the "Teacher B's session URL" part of step 1. The new session in Swagger has UTC times 8 h before the times on the form.

**Dependencies:** Task 2

**Files likely touched:**
- `src/Klasly.Web/ViewModels/StartSessionViewModel.cs`, `SessionPageViewModel.cs` (new)
- `SessionRepository` / `SessionService` (modified: create, open, get)
- `src/Klasly.Web/Areas/Teacher/Controllers/SessionsController.cs` (new: `Start`, `Details`)
- `Areas/Teacher/Views/Classes/Details.cshtml` (modified: form), `Areas/Teacher/Views/Sessions/Details.cshtml` (new)

**Estimated scope:** Medium (5 files)

---

## Task 4: Open a Scheduled session; End session

**Description:** On the session page, a Scheduled session gets an **Open** button (`POST Sessions/Open/{id}`). An Open session gets an **End session** button (`POST Sessions/Close/{id}`) with a `confirm()`, placed away from where the QR will be. Both redirect back to the page. A 400 or 409 from the API becomes a TempData error.

**Acceptance criteria:**
- [ ] Open on a Scheduled session (created through Swagger) → status Open.
- [ ] End session → `confirm()` → status Closed. If the teacher cancels the confirm, nothing changes.
- [ ] Opening or closing a session that another tab already closed → the API's message as a TempData error, not an error page.

**Verification:**
- [ ] Build succeeds: `dotnet build Klasly.slnx`
- [ ] Manual check: smoke step 8 (without the QR). Close it with two tabs open to get the 409 message.

**Dependencies:** Task 3

**Files likely touched:**
- `SessionRepository` / `SessionService` (modified: close)
- `Areas/Teacher/Controllers/SessionsController.cs`, `Areas/Teacher/Views/Sessions/Details.cshtml` (modified)

**Estimated scope:** Small (3 files)

---

## Task 5: QR + roster partial, drawn on the server

**Description:** Add QRCoder 1.8.1 (needs open question 1 approved). `GetLiveAsync` gets the QR token (a `409` means closed, so no QR) and the roster. It turns the token into an inline SVG string. `_Live.cshtml` renders the QR panel, the "x of y checked in · n Present · m Late" summary inside `aria-live="polite"`, and the roster table, with `data-closed="true"` when there is no QR. `Sessions/Details` renders `_Live` for Open and Closed sessions. `GET Sessions/Live/{id}` returns the same partial (it is polled in Task 6).

**Acceptance criteria:**
- [ ] An Open session shows a QR at least 320 px square, dark modules on a white background with a quiet zone, readable in both light and dark mode, with "Changes every 30 s" under it. The token text is not in the HTML except as SVG paths.
- [ ] A phone QR reader decodes the QR to the same token as `GET /api/sessions/{id}/qr` at that moment.
- [ ] The roster lists every Active student with student no., name, a status badge (Present green / Late amber / "Not yet" grey) and the Manila scan time. The summary counts are correct. A Closed or past-end session shows "This session has ended." with the final roster and no QR.

**Verification:**
- [ ] Build succeeds: `dotnet build Klasly.slnx`; `dotnet list Klasly.slnx package` shows QRCoder 1.8.1 as the only package in `Klasly.Web`
- [ ] Manual check: decode with a phone. Scan with curl using a student's JWT, refresh the page, and the row is Present. Smoke step 4 needs a manual refresh until Task 6.

**Dependencies:** Task 3 (build it after Task 4, because they edit the same files)

**Files likely touched:**
- `src/Klasly.Web/Klasly.Web.csproj` (modified: QRCoder)
- `src/Klasly.Web/ViewModels/SessionLiveViewModel.cs` (new)
- `SessionRepository` / `SessionService` (modified: qr, attendance, `GetLiveAsync`, `QrSvg`)
- `Areas/Teacher/Controllers/SessionsController.cs` (modified: `Live`), `Areas/Teacher/Views/Sessions/_Live.cshtml` (new), `Sessions/Details.cshtml` (modified)

**Estimated scope:** Medium (5 files)

### Checkpoint B: after Tasks 3–5
- [ ] `dotnet build Klasly.slnx` clean
- [ ] Teacher goes from login to a scannable QR in three clicks (class → Start session → page)
- [ ] The QR decodes correctly on a real phone; a curl scan shows after a refresh
- [ ] Smoke steps 3 and 8 pass
- [ ] Review with the team before continuing

---

## Task 6: Polling script

**Description:** Add about 25 lines of plain JavaScript to `Sessions/Details`, in its `Scripts` section, only when the session is Open. Every 5 s it calls `fetch("Live/{id}", { redirect: "manual" })`:
- OK → swap the panel's HTML, hide "Reconnecting…", and stop the timer if the new HTML has `data-closed="true"`;
- an opaque redirect (401/403) → `location.reload()`;
- any other failure or a network error → show "Reconnecting…", keep the last HTML, and keep trying.

**Acceptance criteria:**
- [ ] The QR changes within 5 s of each 30 s step, and a curl scan appears within 5 s, with no page reload.
- [ ] The session closes (End session in another tab, or its end time passes) → within 5 s, "This session has ended." appears, and the network tab shows no more polls.
- [ ] API stopped → "Reconnecting…" and the last QR stays; API started again → the page recovers by itself. JWT expired → the browser goes to the login page with "Your session ended, please log in again.", and after login it returns to the session page.

**Verification:**
- [ ] Build succeeds: `dotnet build Klasly.slnx`
- [ ] Manual check: smoke steps 4–7, 9 and 10 (step 10 with `Jwt:ExpiryMinutes` = 1 in the API, set back afterwards)

**Dependencies:** Task 5

**Files likely touched:**
- `src/Klasly.Web/Areas/Teacher/Views/Sessions/Details.cshtml` (modified: `Scripts` section + "Reconnecting…" element)

**Estimated scope:** Extra small (1 file)

---

## Task 7: Accessibility, `/impeccable`, full smoke test, reports

**Description:** Run `/impeccable` on the four Teacher views and fix what it finds. Check keyboard and screen-reader behaviour. Run all 12 smoke-test steps from SPEC.md. Write the three reports and the INDEX line.

**Acceptance criteria:**
- [ ] Keyboard only: tab to Start session, Open and End session. A screen reader announces summary changes, not the whole table. The QR is readable in light and dark mode.
- [ ] `/impeccable` findings are fixed, or listed in the report with the reason they were not fixed.
- [ ] All 12 smoke steps pass. `Klasly.Web.csproj` still has no EF Core package and no `Klasly.Core` / `Klasly.Infrastructure` reference.

**Verification:**
- [ ] Build succeeds: `dotnet build Klasly.slnx`; `dotnet list Klasly.slnx package` shows no Microsoft package below 10.x
- [ ] Manual check: smoke steps 11–12, plus a quick re-run of steps 1–10
- [ ] Reports in `docs/reports/<yyyy-MM-dd-HHmm>-milestone-7-teacher-session-page/`, one line added to `docs/reports/INDEX.md`

**Dependencies:** Task 6

**Files likely touched:**
- `Areas/Teacher/Views/**` (polish only)
- `docs/reports/<folder>/DEVELOPER-DOCS.md`, `HOW-TO-USE.md`, `FLOWCHARTS.md`, `docs/reports/INDEX.md`

**Estimated scope:** Medium

### Checkpoint C: complete
- [ ] Every success criterion in SPEC.md is met
- [ ] No API or database change; QRCoder is the only new package
- [ ] Ready for `/review`
