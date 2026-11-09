# Spec: Milestone 7 — Web MVC Teacher session page (rotating QR + live attendance)

> Status: **Draft — waiting for approval** (2026-11-09). Answer the 3 open questions at the bottom, or approve them as recommended. Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` (§27.8 "QR attendance check-in") → `CLAUDE.md` (§3.4 Web MVC) → this spec.
> Milestone 6's spec (Web API sessions) is in git history (commit `479dcc6`) and in `docs/reports/2026-11-09-2144-milestone-6-api-sessions/`.

## Assumptions (correct these before we build)

1. **Web MVC only.** No API change: every endpoint this page needs was built in Milestone 6. No database change. No Android code (Milestone 8).
2. **A `Teacher` Area, built like the Admin Area from Milestone 5:**
   - `[Area("Teacher"), Authorize(Roles = "Teacher")]` on each controller;
   - same Controller → Service → API-backed Repository → typed `HttpClient` chain;
   - the same `ApiExceptionFilter` (401 → login, 403 → Access denied, 404, API down page);
   - TempData alerts and `[ValidateAntiForgeryToken]` on every POST.
3. **Home redirect:** a Teacher who opens `/` goes to **My classes** (`/Teacher/Classes`), the same way an Admin goes to `/Admin`. The "coming soon" welcome page then shows only for Parents.
4. **The browser never talks to the API** (CLAUDE.md §3.3, CORS stays off). The page's JavaScript polls an **MVC action** on the same site. That action calls the API server-side with the JWT from the cookie. The JWT never reaches page JavaScript.
5. **Times are shown in Asia/Manila** (CLAUDE.md §3.1), for example "Nov 10, 9:00 AM". The API sends and receives UTC.
   - The Start-session form takes Manila local time.
   - The service sends it as `+08:00` (`DateTimeOffset`).
   - One small helper does both conversions with `TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")`.
6. **"Session closed" covers more than Status = Closed.** The Milestone 6 API treats an Open session past its end time as closed. When the QR call answers `409`, the page stops polling and shows "This session has ended." It still shows the final roster.
7. **Minimal JavaScript.** About 25 lines of plain `fetch` + `setInterval` in the session view's `Scripts` section: no framework and no SignalR. Everything else is server-rendered Razor.
8. **No test project** (same as Milestones 3–6). The smoke test is done by hand in the browser, with the API and the website running. Automated tests come in Milestone 14.
9. **Out of scope:**
   - attendance corrections and Absent marking (Milestone 9);
   - editing or cancelling a session;
   - Parent pages (Milestone 13);
   - the admin "Reset phone" button (Swagger for now);
   - a full-screen/projector mode beyond a large QR (the browser's F11 works);
   - CSV export.

## Objective

A teacher logs in on the website, picks one of their classes and starts a session. The page then shows a QR code that changes every 30 seconds for students to scan. Next to it, a list fills in with Present / Late as students check in, without reloading the page. When class ends, the teacher clicks **End session**.

This is the web half of the vertical slice. After Milestone 8 (Android scanner), the whole flow works: the teacher opens a session, a student scans, the teacher sees the check-in.

Functional requirements covered: guide §27.9 **#3** (open a session that shows a signed QR refreshing every 30 s) and the teacher side of **#4** (see Present / Late as scans arrive; the list "updates by polling", guide §27.8).

### User stories
- As a **teacher**, I see only my own classes, active term first.
- As a **teacher**, I open a class and see its past and upcoming sessions with their status.
- As a **teacher**, I start a session now. The start, end (default 90 min) and minutes-until-Late (default 15) come prefilled, and I can change the end and the Late minutes.
- As a **teacher**, while the session is open, I see a large QR that changes every 30 s and a roster: who is Present, Late, or not yet scanned, and how many have checked in.
- As a **teacher**, I end the session, and the page shows the final roster.
- As a **teacher**, I open a Scheduled session (for example, one created earlier) with an **Open** button.
- As **another teacher**, a URL to someone else's session gives me "Access denied".

## Pages and actions

All under `/Teacher/...`; POSTs carry the anti-forgery token.

| Route | What it does | API calls |
|---|---|---|
| `GET /Teacher/Classes` | **My classes**: table with subject code + title, section, term, schedule, room, enrolled count, and a "Sessions" button | `GET api/teacher/classes` |
| `GET /Teacher/Classes/Details/{id}` | Class header, a **Start session** form, and the session list (newest first): date, start–end (Manila), status badge, link | `GET api/teacher/classes` (header; not in the list → 404), `GET api/classes/{id}/sessions` |
| `POST /Teacher/Sessions/Start` | ⓘ Creates and opens the session in one step, then redirects to its page. A `400` from the API shows on the form | `POST api/classes/{id}/sessions`, then `POST api/sessions/{id}/open` |
| `GET /Teacher/Sessions/Details/{id}` | **The session page.** Scheduled: details + **Open** button. Open: QR panel + live roster + **End session**. Closed/ended: final roster | `GET api/sessions/{id}`, plus the live part below |
| `GET /Teacher/Sessions/Live/{id}` | ⓘ Partial view the page polls: the QR (while open) + roster + "x of y checked in" | `GET api/sessions/{id}/qr` (409 → closed state), `GET api/sessions/{id}/attendance` |
| `POST /Teacher/Sessions/Open/{id}` | Opens a Scheduled session → back to the page. `400` / `409` → TempData error | `POST api/sessions/{id}/open` |
| `POST /Teacher/Sessions/Close/{id}` | "End session" (with `confirm()`) → back to the page, which shows the final roster | `POST api/sessions/{id}/close` |

### Session page behaviour (Open session)

- **QR panel:** the QR code drawn as inline SVG, at least 320 px square, with dark modules on white (scanners need contrast in both themes). Under it, small text: "Changes every 30 s". The token text itself is **not** shown, so nobody types it in.
- **Polling:** every **5 s** ⓘ, the page fetches `Live/{id}` and swaps the panel's HTML.
  - The QR changes within 5 s of each new 30-s step. The API accepts the previous step for another 30 s, so a phone that scanned an older QR still works.
  - If the poll answer is a **redirect to login** (the JWT expired), the page goes to the login page, which shows the "session ended" message.
  - If the session is **closed or ended** (`409` from the QR call), the partial shows "This session has ended." with the final roster, and polling stops.
  - If the **network or API fails**, the page keeps the last QR and roster, shows "Reconnecting…", and keeps trying.
- **Roster:**
  - one row per Active student, by name: student no., name, status badge (Present green / Late amber / "Not yet" grey) and scan time (Manila);
  - a summary line "12 of 30 checked in · 10 Present · 2 Late" inside an `aria-live="polite"` region (only the summary is announced, not the table).
- The **End session** button sits away from the QR, so it doesn't get clicked by accident.

## Commands

```bash
dotnet add src/Klasly.Web package QRCoder --version 1.8.1        # only if open question 1 is approved
dotnet build Klasly.slnx
dotnet list Klasly.slnx package
dotnet run --project src/Klasly.API --launch-profile https       # https://localhost:7256
dotnet run --project src/Klasly.Web --launch-profile https       # https://localhost:7144
```

## Project structure (files this milestone touches)

```
src/Klasly.Web/
  Models/Api/SessionDtos.cs                      (new) CreateSessionRequest, SessionResponse, QrTokenResponse, SessionAttendanceRow
  Repositories/SessionRepository.cs + Interfaces/ISessionRepository.cs   (new) the 8 teacher API calls
  Services/SessionService.cs + Interfaces/ISessionService.cs             (new) page models, start = create + open, live = QR + roster, QR → SVG
  Infrastructure/ManilaTime.cs                   (new) UTC ↔ Asia/Manila
  ViewModels/TeacherClassViewModel.cs, StartSessionViewModel.cs, SessionPageViewModel.cs, SessionLiveViewModel.cs   (new)
  Areas/Teacher/Controllers/ClassesController.cs, SessionsController.cs  (new)
  Areas/Teacher/Views/_ViewImports.cshtml, _ViewStart.cshtml,
        Classes/Index.cshtml, Classes/Details.cshtml,
        Sessions/Details.cshtml, Sessions/_Live.cshtml                    (new)
  Controllers/HomeController.cs                  (modified) Teacher → /Teacher/Classes
  Views/Shared/_Layout.cshtml                    (modified) Teacher nav: "My classes"
  Views/Home/Index.cshtml                        (modified) welcome text for Parents only
  Program.cs                                     (modified) register the repository + service
  Klasly.Web.csproj                              (modified, only if Q1 approved) QRCoder
```

## Code style

Same as Milestone 5: one-line repository methods on the `ApiResponse` helpers, services shape data for views, and controllers catch only `ApiException.IsFormError` on form posts. The live action:

```csharp
// SessionsController (Teacher area)
[HttpGet]
public async Task<IActionResult> Live(int id, CancellationToken ct) =>
    PartialView("_Live", await sessionService.GetLiveAsync(id, ct));

// SessionService
public async Task<SessionLiveViewModel> GetLiveAsync(int sessionId, CancellationToken ct)
{
    QrTokenResponse? qr = null;
    try { qr = await sessions.GetQrAsync(sessionId, ct); }
    catch (ApiException e) when (e.StatusCode == StatusCodes.Status409Conflict) { } // closed or past its end
    var roster = await sessions.GetAttendanceAsync(sessionId, ct);
    return new SessionLiveViewModel(sessionId, qr is null ? null : QrSvg(qr.Token), roster);
}
```

## Testing strategy

- **Build:** `dotnet build Klasly.slnx` → 0 errors, 0 new warnings. `dotnet list package` → no Microsoft package below 10.x. `Klasly.Web.csproj` still has no EF Core package and no `Klasly.Core` / `Klasly.Infrastructure` reference.
- **Manual smoke test** in the browser, with the API and the website running on LocalDB, using the seed teachers (`rosa.villanueva@klasly.test` for ELECT3, `mark.delossantos@klasly.test`) and students. The phone scan is simulated with curl / Swagger (`POST /api/attendance/scan` with the student's JWT and the token read from the page's QR, or from `GET /api/sessions/{id}/qr`).
  1. Teacher A logs in → lands on **My classes** with only their classes. Teacher B's session URL → Access denied. An Admin opening `/Teacher/Classes` → Access denied. Parent → welcome page.
  2. Class details shows the sessions created in Milestone 6 with Manila times and status badges.
  3. Start session (defaults) → redirected to an Open session page with a QR and a roster of "Not yet" rows. End before start → the API's message on the form, values kept.
  4. The QR changes within 5 s after each 30-s boundary (watch two boundaries). A phone app (or any QR reader) decodes it to the same token `GET /api/sessions/{id}/qr` returns.
  5. A student scan (curl) → within 5 s the row turns **Present** with the Manila time, and the summary updates. A scan made 30 min after start, on a session created with start = now − 30 min (via Swagger), shows **Late**.
  6. **End session** → `confirm()` → the page shows "This session has ended." with the final roster, and the browser's network tab shows polling stopped.
  7. A session left open past its end time (created via Swagger ending in 2 min) → within 5 s of the end the page switches to "This session has ended." on its own.
  8. A Scheduled session (created via Swagger) → **Open** button → QR appears. Opening a closed session (two tabs) → the API's message as a TempData error.
  9. Stop the API while the page is open → "Reconnecting…", the last QR stays. Start the API → the page recovers by itself.
  10. JWT expiry (temporarily set `Jwt:ExpiryMinutes` = 1 in the API, as in Milestone 5) → the next poll sends the browser to the login page with "Your session ended, please log in again."
  11. Keyboard only: tab to Start, Open and End session; a screen reader announces the summary changes. The QR is readable in both light and dark mode.
  12. `/impeccable` run on the four views; findings fixed or listed in the report.

## Boundaries

- **Always:**
  - Controller → Service → API-backed Repository (guide §2);
  - no database or `Klasly.Core` access from the Web;
  - `[ValidateAntiForgeryToken]` on every POST;
  - show the API's error message on failure;
  - Manila time for display and UTC on the wire;
  - `dotnet build` after each change; the three reports when built.
- **Ask first:**
  - any package other than QRCoder (Q1);
  - any API or database change;
  - any page not in the table above;
  - more JavaScript than the polling script.
- **Never:**
  - call the API from browser JavaScript, or turn on CORS;
  - expose the JWT or the QR token text to page scripts beyond the SVG drawing;
  - poll faster than every 5 s;
  - trust the browser's clock for Present / Late (the API decides).

## Success criteria

- [ ] All 12 smoke-test steps behave as written.
- [ ] A teacher can go from login to a scannable, rotating QR in three clicks (class → Start session → page).
- [ ] A scan shows on the teacher's roster within 5 s, with no page reload.
- [ ] The page stops polling on its own when the session is closed or past its end.
- [ ] Only the class's own teacher can see or control its sessions (the API enforces it; the website shows Access denied).
- [ ] No API or database change; only the QRCoder package added (if Q1 approved); every Microsoft package is 10.x.
- [ ] Reports written; `docs/reports/INDEX.md` updated.

## Open questions (my recommendation first)

1. **How is the QR drawn?** Nothing in the solution draws QR codes yet.
   **Recommended:** the **QRCoder** NuGet package (v1.8.1, MIT, about 95 M downloads, pure .NET with no image library). The service turns the token into an **SVG string** on the server, and the partial view outputs it inline. The token never touches page JavaScript, and nothing is loaded from a CDN.
   *Alternatives:* a small JavaScript QR library copied into `wwwroot/lib` (the token would have to reach page JavaScript); or a CDN script (an outside dependency at class time).
2. **How often does the page poll?**
   **Recommended:** **one poll every 5 s** that returns both the QR and the roster, so there is a single timer and a single partial view. That costs 2 API calls per 5 s per open session page, which is fine for a class.
   *Alternative:* fetch the QR exactly at `expiresAtUtc` and the roster on its own 5-s timer: more precise rotation, but two timers and more JavaScript.
3. **How does a teacher start a session?**
   **Recommended:** a **Start session** form on the class page, prefilled with start = now, end = now + 90 min and Late after 15 min. One click **creates and opens** the session and goes to its page. Scheduled sessions (made through Swagger) still get an **Open** button.
   *Alternative:* a separate "Schedule a session" form for later dates plus an Open step. That is more pages, and nothing in the guide asks for scheduling ahead.
