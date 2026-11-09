# Milestone 7 · Tasks 2–7 — Teacher session page (rotating QR + live attendance)

## Summary
Asked: build and test Milestone 7 up to Checkpoint C (Tasks 2–7 of `tasks/todo.md`; Task 1 has its own report, `2026-11-09-2250-milestone-7-task-1-teacher-classes`). Asking to build through Checkpoint C was taken as approval of SPEC.md open question 1 (QRCoder) and the other two recommended answers.
Done: a teacher opens a class, sees its sessions in Manila time, and clicks **Start session**. That one click creates and opens the session. The session page shows a server-drawn QR that changes every 30 s and a roster that updates every 5 s without reloading. **End session** closes it and shows the final roster. Scheduled sessions get an **Open** button. Expired logins, API outages and sessions that end by time are all handled on the page.

## Projects touched
WEB MVC only. No API, EF Core, database, Worker or Android change. One new package: **QRCoder 1.8.1** (MIT, pure .NET, draws SVG; nothing in ASP.NET Core draws QR codes).

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Klasly.Web.csproj` | modified | QRCoder 1.8.1 |
| `src/Klasly.Web/Models/Api/SessionDtos.cs` | added | Web copies of the API's session, QR and roster shapes (statuses as strings). `SessionResponse.IsLive` / `DisplayStatus` = "Ended" for an Open session past its end, matching the API's rule |
| `src/Klasly.Web/Infrastructure/ManilaTime.cs` | added | UTC ↔ Asia/Manila: `ToLocal`, `ToOffset` (+08:00 for the create request), `Format`, `FormatTime` |
| `src/Klasly.Web/Repositories/(Interfaces/I)SessionRepository.cs` | modified | The 8 teacher API calls, one line each |
| `src/Klasly.Web/Services/(Interfaces/I)SessionService.cs` | modified | Class page model, Start = create + open, session page, Live = QR + roster, QR → SVG |
| `src/Klasly.Web/ViewModels/TeacherClassViewModel.cs` | added | Class header + sessions + the Start form |
| `src/Klasly.Web/ViewModels/StartSessionViewModel.cs` | added | Start form: end time (Manila) and Late minutes (0–120) |
| `src/Klasly.Web/ViewModels/SessionPageViewModel.cs` | added | Session + its live part (null while Scheduled) |
| `src/Klasly.Web/ViewModels/SessionLiveViewModel.cs` | added | QR SVG (null = closed) + roster + "x of y checked in" summary |
| `src/Klasly.Web/Areas/Teacher/Controllers/ClassesController.cs` | modified | `Details` |
| `src/Klasly.Web/Areas/Teacher/Controllers/SessionsController.cs` | added | `Details`, `Live`, `Start`, `Open`, `Close` |
| `src/Klasly.Web/Areas/Teacher/Views/Classes/Details.cshtml` | added | Class header, Start form, session list |
| `src/Klasly.Web/Areas/Teacher/Views/Shared/_StartSession.cshtml` | added | The Start form. It is in `Shared` because `SessionsController.Start` re-renders the class page after an error |
| `src/Klasly.Web/Areas/Teacher/Views/Shared/_SessionStatusBadge.cshtml` | added | One status → badge colour mapping for both pages |
| `src/Klasly.Web/Areas/Teacher/Views/Sessions/Details.cshtml` | added | Session header, Open/End buttons, live panel, the polling script |
| `src/Klasly.Web/Areas/Teacher/Views/Sessions/_Live.cshtml` | added | QR + summary + roster, swapped every 5 s |
| `src/Klasly.Web/Areas/Teacher/Views/_ViewImports.cshtml` | modified | `@using Klasly.Web.Infrastructure` (for `ManilaTime`) |
| `src/Klasly.Web/wwwroot/css/site.css` | modified | `.qr-code`: 320–440 px square, white background |
| `tasks/plan.md`, `tasks/todo.md` | modified | Tasks 2–7 and checkpoints ticked |

## Key code

**Start = create + open** (`SessionService.StartAsync`). The session starts "now". If only the create works, the page still opens on the new session, which shows its Open button:
```csharp
var created = await sessions.CreateAsync(model.ClassId,
    new CreateSessionRequest(DateTimeOffset.UtcNow, ManilaTime.ToOffset(model.EndTime!.Value), model.LateAfterMinutes!.Value), ct);
try { await sessions.OpenAsync(created.SessionId, ct); return (created.SessionId, null); }
catch (ApiException ex) when (ex.IsFormError) { return (created.SessionId, ex.Message); }
```

**Live part** (`SessionService.BuildLiveAsync`). A `409` from the QR call means closed or past its end. The QR is drawn on the server:
```csharp
try { qr = await sessions.GetQrAsync(sessionId, ct); }
catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status409Conflict) { }
...
using var data = generator.CreateQrCode(token, QRCodeGenerator.ECCLevel.M);
return new SvgQRCode(data).GetGraphic(10, "#000000", "#ffffff", drawQuietZones: true, SvgQRCode.SizingMode.ViewBoxAttribute);
```

**Polling script** (`Sessions/Details.cshtml`, only rendered while the session is live):
- `fetch(url, { redirect: 'manual' })`: a 401 or 403 comes back as an opaque redirect → `location.reload()`. The browser then reloads the *page*, so the login returns to the session page, and the "Your session ended" message isn't used up by the fetch.
- Not OK, or a network error → show "Reconnecting…" and keep the last QR and roster.
- `data-closed="true"` → wait 3 s, then reload once. The reloaded page shows the final state and has no poller.
- Each poll starts 5 s after the previous one *finishes* (a `setTimeout` chain). The first version used `setInterval`. During the API-restart test the calls took 5–12 s, so the polls piled up, and an old answer could have overwritten a newer one.
- A visually hidden `aria-live="polite"` element gets the summary text, so a screen reader announces "2 of 6 checked in…" but not the whole table.

## Database changes
None.

## How it was verified
- `dotnet build Klasly.slnx`: **0 errors, 0 warnings**, after every task.
- `dotnet list Klasly.slnx package`: every Microsoft package is 10.0.14. `Klasly.Web` has only QRCoder 1.8.1 and still has no EF Core package and no `Klasly.Core` / `Klasly.Infrastructure` reference.
- No test project exists (spec assumption 8), so there was no automated RED/GREEN step. Every SPEC.md smoke step was run by hand in the in-app browser, with the API and the website on LocalDB:

| Step | Result |
|---|---|
| 1 Role routing | Teacher → My classes, Admin/Parent → Access denied on `/Teacher/...` (Task 1 report). Mark on Rosa's session 12 → Access denied, and he sees only his 2 classes |
| 2 Class details | Sessions newest first. `01:00Z` → "9:00 AM", `13:44Z` → "9:44 PM". Open-but-past-end shows **Ended**. Mark's class 3 → Access denied, unknown id → 404 |
| 3 Start session | Defaults → session 12 opened, page shows **Open**. End in the past → API message "The end time must be after the start time." on the form, values kept (end 08:00, Late 20) |
| 4 QR rotation + decode | Test script: render the page's SVG, decode it with zxing-cpp, compare with `GET /api/sessions/14/qr` every 10 s for 90 s. **9/9 matched**, and the token changed at 3 consecutive 30 s boundaries |
| 5 Scan → roster | Aira scanned with curl (registered device `phone-2`): row turned **Present 11:29 PM** within 5 s, no reload (8 polls in ~40 s), summary and aria-live text updated. Session with start = now − 30 min: Carla → **Late** (amber) |
| 6 End session | `confirm()` text shown → "Session ended.", badge Closed, final roster (6 rows), no poller script on the page |
| 7 Ends by time | Session 16 ending in 2 min: within ~5 s of the end, "This session has ended." appeared. The page then reloaded to **Ended**, no QR, no poller |
| 8 Open Scheduled / stale tab | Session 2 (Scheduled) → Open → QR. Stale tab opening the already-closed session 13 → TempData error "Session is closed". (Closing twice is not an error: the API treats it as success) |
| 9 API down | "Reconnecting…" shown, last QR kept. API restarted → recovered by itself (first calls after the restart take 5–12 s while the API warms up) |
| 10 JWT expiry | API run with `Jwt__ExpiryMinutes=1` (environment variable, no file changed): the page went to `/Account/Login?ReturnUrl=/Teacher/Sessions/Details/14` with "Your session ended, please log in again.". Logging in returned to the session page with its QR |
| 11 Keyboard / reader / themes | Tab reaches Start session, Open and End session, with a visible focus ring. The aria-live element gets the summary (checked in the DOM, not with a real screen reader). The site has no dark mode, and the QR has a fixed white background |
| 12 `/impeccable` | Audit below |

**`/impeccable audit`:**
- The detector reported no findings on all Teacher views and `site.css`.
- Rendered check at 375 px: no horizontal scroll, QR 354 px wide. At 1024 px it is 376 px, with the QR in the left column and End session at the top right.
- Fixed: student numbers and scan times wrapped onto two lines on phones (now `text-nowrap`). The session page had no headings after the `h1` (added visually hidden `h2`s "Check-in QR code" and "Attendance", for screen-reader navigation).
- Left as is: on phones, **End session** sits about 50 px above the QR. Its `confirm()` prevents an accidental end, and teachers project from a laptop, where it is far from the QR.

## Known limits / TODO
- **Not checked with a real phone camera.** The QR was decoded by zxing-cpp from the page's own SVG, which is the same library family many scanner apps use. Point a phone at the projected QR once (Milestone 8 does this for real).
- **Test data left in LocalDB:**
  - sessions 12–16 (14 is still Open until 12:56 AM Manila);
  - attendance for Aira (session 14) and Carla (session 15);
  - **session 2 (Jan 10 2027, from Milestone 6) is now Open instead of Scheduled**, because step 8 opened it. It will show "Show QR" until Jan 2027. That is handy for Android testing; close it from its page if you don't want it.
- An Open session past its end still shows **End session**. Clicking it formally closes the session, which the API allows.
- `.claude/launch.json` (starts the API and the website from the Claude app) is still uncommitted.
