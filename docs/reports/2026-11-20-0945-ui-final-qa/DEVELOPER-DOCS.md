# UI redesign M8 — final QA and summary

## Summary
Final milestone of the Web UI/UX redesign on branch `ui-redesign` (SPEC.md rev 4). A full sweep of every page found and fixed the last phone issues (outer cell padding only from 576 px; secondary columns moved under the main cell in four tables; correction form wraps). All SPEC success criteria are now met except one impeccable heuristic, which is left standing with a reason. Nothing is merged or deployed (demo 2026-11-21).

## Projects touched
WEB MVC only (Android icon drawables changed in M1).

## What the redesign delivered (M0–M8)
| Milestone | Commit | Result |
|---|---|---|
| M0 | `ad30bec`, `81530a1`, `d252316` | SPEC approved (rev 4), plan, form baseline for 25 page states |
| M1 ui-brand | `8c27d63` | K + check mark from the team's concept; logo, reverse, favicon SVGs; Inter 4.1 + OFL; Android adaptive icon |
| M2 ui-foundation | `0bd7082`, `fcd3258` | Tokens, type scale, layout/nav, `_StatusBadge`, submit + unsaved guards, `DESIGN.md`; table label overflow fix |
| M3 ui-message-pages | `a871188` | Styled Not found (404 kept), Error, Access denied, Server unavailable |
| M4 ui-teacher | `37081eb` | Live session first, per-form errors, return to corrected row, big count, grade save bar |
| M5 ui-parent | `86da208` | One-line answer, at-risk first, whole-row child links |
| M6 ui-admin | `9f7f6b2` | Row-action convention, badges, breadcrumbs, grouped account form, enroll first |
| M7 ui-auth-polish | `ebb5701` | Login panel with logo, in-panel alerts, "Logging in…" |
| M8 final QA | this commit | Phone table fixes, criteria ticked, this report |

## Files changed in M8
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/wwwroot/css/site.css` | modified | Outer cell inset (1 rem) inside table panels, from 576 px only |
| `Areas/Teacher/Views/Classes/Details.cshtml` | modified | Phones: max score/weight under the assessment title, time under the session date, date may wrap |
| `Areas/Teacher/Views/Sessions/_Live.cshtml` | modified | Correct form wraps (select + Save) on narrow screens |
| `Areas/Parent/Views/Children/Details.cshtml` | modified | Phones: grade weight under the assessment |
| `SPEC.md` | modified | Success criteria ticked with notes |

## Server-side footprint of the whole redesign
`git diff --stat dev` on server code: 5 files, +36 / −2 lines — exactly the three approved changes (status-code re-execute + `HomeController.StatusCode`, `MoveSummaryErrorsTo` + two calls, correction redirect fragment). No API, database or package change.

## Database changes
None.

## How it was verified
- `dotnet build Klasly.slnx`: **0 warnings, 0 errors**. `dotnet test Klasly.slnx`: **163 passed, 0 failed**.
- **Form contract** (fetch + DOMParser, hashed against `docs/ui-ux/baseline-forms.json`): login, 18 admin and 6 teacher page states identical; Class details has the same two forms in a new order (Enroll first, planned). Every POST form has `__RequestVerificationToken`.
- **Responsive sweep** at 320/375/768/1440 px across login, access denied, not found, 14 admin, 5 teacher and 2 parent pages: no page scrolls sideways; no table panel scrolls except Failed emails (allowed); body text 16 px everywhere.
- **Keyboard:** first Tab shows "Skip to content" with a 2 px teal outline; Enter moves focus to `main#main`; next Tab lands on the first content link with a visible outline.
- **Inter** loads (`document.fonts.check('16px Inter')` true). Contrast measured in M2 (body 11.4:1, muted 5.1:1 on canvas, primary 6.3:1, input borders 3.0:1).
- **Live session:** QR on white, `Live` polled every ~5 s, API stopped → "Reconnecting… The QR and list below may be out of date." with the last QR kept.
- **impeccable detect:** `site.css` clean. On saved rendered teacher pages, one heuristic remains: `cramped-padding` on `.table-responsive` (the table fills its white panel by design; cell text is inset 16 px from 576 px, 8 px on phones). It can't be suppressed by value; left standing.

## Known limits / TODO
- Android: `./gradlew assembleDebug testDebugUnitTest` passed with the new icon (JDK at `D:Android Studiojbr`); not checked on a device.
- No automated UI tests (Playwright would be a new package).
- `/review` was done as a self-review of the 36-line server diff and the view diffs, not by a separate reviewer agent.
- PR into `dev` only after the demo, on the team's go-ahead.
