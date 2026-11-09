# Implementation Plan: Milestone 7 — Web MVC Teacher session page (rotating QR + live attendance)

> Built from [`SPEC.md`](../SPEC.md) (2026-11-09). **The spec is still a draft.** This plan follows the recommended answer to all 3 open questions (QRCoder, one 5-second poll, Start = create + open). If you choose differently, Tasks 3, 5 and 6 change. Task details, acceptance criteria and verification steps are in [`todo.md`](./todo.md).
> Milestone 6's plan is in git history (commit `479dcc6`).

## Overview

This is the teacher's half of QR check-in, on the website. A teacher sees their own classes, starts a session, and gets a page with a QR code that changes every 30 s and an attendance list that updates every 5 s without reloading. The work is **Web MVC only**: no API change, no database change, one new package (QRCoder).

## Facts from the code that shape the plan

- **All 8 API calls already exist** in `Klasly.API/Controllers/SessionsController.cs` (TeacherOnly policy): `GET api/teacher/classes`, `GET`/`POST api/classes/{id}/sessions`, `GET api/sessions/{id}`, `POST …/open`, `POST …/close`, `GET …/qr`, `GET …/attendance`.
- `api/teacher/classes` already sorts **active term first**, then by subject code and section. The website shows the list in the order it gets it.
- The API returns sessions **newest first** (`SessionRepository.ListByClassAsync`).
- The API sends enums as **strings** (`JsonStringEnumConverter`). The Web DTOs use `string Status`, the same way `EnrollmentResponse` does.
- `GET …/qr` answers `409 "Session is closed"` for anything that is not Open, or is Open but past its end time (`IsOpenNow`). `POST …/close` accepts an Open session even after its end time, and answers `409` for any other status.
- Creating a session checks that the end is after the start and is still in the future. The start may be in the past, which smoke-test step 5 needs.
- `ApiExceptionFilter` turns a 401 into "sign out + TempData message + redirect to login", a 403 into a redirect to Access denied, a 404 into a 404, and "API unreachable" into the `ApiUnavailable` page with status 503. **The Live action reuses this filter, so it needs no error handling of its own.**
- `ApiException.IsFormError` (400/409) is the only thing controllers catch. Admin's `ClassesController.Enroll` already shows the pattern: catch it → TempData error → redirect.
- `Klasly.Web.csproj` has **no packages** today. QRCoder 1.8.1 is the latest version on NuGet (checked 2026-11-09).

## Architecture decisions

1. **One repository and one service for the Teacher area:** `ISessionRepository` (the 8 API calls) and `ISessionService` (view models, start = create + open, live = QR + roster, QR → SVG). The teacher's class list comes from `api/teacher/classes`, so the Admin `ClassRepository` (AdminOnly endpoints) is not reused.
2. **The session page draws the live part on the server the first time.** `Sessions/Details` calls `GetLiveAsync` and renders `_Live` inside the page. The QR and the roster are visible at once, with no blank first 5 s, and a Closed session gets its final roster from the same partial. Only an Open session adds the polling script.
3. **The polling script uses `fetch(url, { redirect: "manual" })`.** A 401 or 403 from the Live action is a redirect. With `"manual"`, the script sees an opaque redirect and calls `location.reload()`. The browser then reloads the *page* (not the partial), so:
   - after login, the teacher comes back to the session page, not to the bare partial;
   - the "Your session ended" TempData message is still there for the login page, because the fetch never loaded the login page.

   Any other non-OK answer (503 API down, 404) → show "Reconnecting…" and keep the last HTML.
4. **The partial reports "closed" with `data-closed="true"`** on its root element. After each swap, the script checks it and stops the timer. No JSON endpoint, no second request.
5. **Manila time helper:** `ManilaTime.ToLocal(DateTime utc)` for display, and `ManilaTime.ToOffset(DateTime local)` → `DateTimeOffset` with +08:00 for the create request. The zone comes from `TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")`. Manila has no daylight saving, so the conversion is always exact.
6. **If Start fails halfway** (the create works but the open fails), the session stays Scheduled. The page redirects to it with the API's message as a TempData error, and the **Open** button is there. Nothing is lost, and nothing is retried automatically.
7. **Close and Open catch `IsFormError`** (for example, a session already closed in another tab) and show it as a TempData error, the same way Admin's `Enroll` does.

## Dependency graph

```
Task 1  Teacher area + My classes        (repository, service, area shell, home redirect, nav)
   │
   └── Task 2  Class details + session list   (session DTOs, ManilaTime)
          │
          └── Task 3  Start session → session page (static)
                 │
                 ├── Task 4  Open (Scheduled) and End session buttons
                 │
                 └── Task 5  QR + roster partial, drawn on the server   ← QRCoder (main risk)
                        │
                        └── Task 6  5-second polling script
                               │
                               └── Task 7  Accessibility, /impeccable, full smoke test, reports
```

Tasks 4 and 5 both depend only on Task 3. They touch the same controller and view, so build them one after the other, not in parallel.

## Task list

### Phase 1: Teacher can find their sessions
- [x] Task 1: Teacher area + My classes page
- [ ] Task 2: Class details with the session list (Manila times)

### Checkpoint A (after Tasks 1–2)
- [ ] `dotnet build Klasly.slnx` clean
- [ ] Smoke steps 1–2 pass (role routing, Access denied, Manila times)

### Phase 2: Vertical slice, no live updates yet
- [ ] Task 3: Start session (create + open) → session page
- [ ] Task 4: Open a Scheduled session; End session
- [ ] Task 5: QR + roster partial, drawn on the server

### Checkpoint B (after Tasks 3–5)
- [ ] A phone QR reader decodes the page's QR to the same token as `GET /api/sessions/{id}/qr`
- [ ] A curl scan shows up after a manual page refresh
- [ ] Smoke steps 3 and 8 pass

### Phase 3: Live, then finish
- [ ] Task 6: Polling script (5 s, reconnect, auto-stop, login redirect)
- [ ] Task 7: Accessibility + `/impeccable` + full smoke test + reports

### Checkpoint C (complete)
- [ ] All 12 smoke-test steps and every success criterion in SPEC.md are met
- [ ] `dotnet list package`: QRCoder is the only new package; no Microsoft package below 10.x
- [ ] Reports written, `docs/reports/INDEX.md` updated, ready for review

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| The QR doesn't scan: too small, low contrast in dark mode, or damaged by Bootstrap CSS on the `<svg>` | High (it breaks the whole flow) | Task 5 is checked with a real phone before the polling is built (Checkpoint B). The QR has a white background with a quiet zone, is at least 320 px, and uses error-correction level M. |
| A poll that follows the redirect to login uses up the TempData message, and after logging in the teacher lands on the bare partial | Medium | `redirect: "manual"` + `location.reload()` (decision 3). Smoke step 10 checks it. |
| `Asia/Manila` isn't found on a Windows machine without ICU | Low | .NET 10 on Windows 10 1903+ uses ICU, so the name works. If it throws, use `"Singapore Standard Time"` (also UTC+8, no DST) and add a `shortcut:` comment. |
| The session ends while the page is open, but the poll still renders a QR between the end and the next poll | Low | The API decides (`IsOpenNow`). The page switches within 5 s, and a scan after the end gets `409` anyway. |
| `datetime-local` posts a time with no time zone | Low | The form documents "Manila time". `ManilaTime.ToOffset` adds +08:00. The API checks the order of start and end. |
| Polling 2 API calls every 5 s for each open page | Low | Fine for one class (the spec accepts it). The timer stops when the session is closed. |

## Open questions

1. **Approve SPEC.md's open questions 1–3 as recommended?** This plan assumes yes. Question 1 (adding the QRCoder package) needs an explicit yes under CLAUDE.md §4.
2. Should the spec's status be changed to "Approved" in the same commit as this plan? That is how Milestone 6 did it.
