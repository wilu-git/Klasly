# Plan: Klasly Web UI/UX redesign, in milestones

## Context
`SPEC.md` (rev 3) redesigns `src/Klasly.Web`:
- the Klasly brand (teal `#126B64`, navy `#17324D`, Inter, K + check logo);
- one design system on top of Bootstrap 5.3.3;
- one shared layout;
- page fixes for all 22 pages listed in `docs/ui-ux/UI_UX_HANDOFF.md`.

The goal is one coherent app that keeps every route, form field, validation rule, auth check and the live-session contract. The demo is on 2026-11-21, so all work stays on **`ui-redesign`**, which you chose to keep as the branch. Its parent is `dev` at `f92929a`, and it's already checked out. Nothing is merged or deployed until after the demo.

My picks for the last three spec questions are adopted:
1. Muted text `#5A6A80`; form-control borders `#8696A7`.
2. Download Inter v4.1 (`InterVariable.woff2` and the OFL license) from the official rsms/inter GitHub release.
3. The Android monogram as an adaptive-icon vector drawable, built by a teammate (this PC has no JDK).

## How every milestone runs
Each milestone is a thin slice that must pass all of these before the next one starts:
1. **Read:** `graphify query` first, then the handoff section, then the views, controllers and view models it names.
2. **Implement** only that milestone's scope.
3. **Build and test:** `dotnet build Klasly.slnx` with 0 warnings, then `dotnet test` all green. The test project doesn't reference Klasly.Web, so this proves only that the server side still builds and passes.
4. **Browser check** with `preview_start` `api` then `web`, logged in with the accounts in `docs/TEST-ACCOUNTS.md`:
   - screenshots at 375, 768 and 1440 px (320 px for layout milestones);
   - no sideways scroll;
   - no console errors;
   - each changed page's success and error paths.
5. **Form-contract diff** against the M0 baseline: the same action, method, input names and anti-forgery token for every form.
6. **Design check:** `/impeccable` on the changed views, which must come back clean.
7. **Housekeeping:** `graphify update .`, then the CLAUDE.md §5 reports in `docs/reports/<yyyy-MM-dd-HHmm>-ui-<module>/` and a new line in `INDEX.md`.
8. **Commit** on `ui-redesign`: one commit per milestone, conventional message, no Claude attribution (memory rule). Never push.

Review gates where I stop and wait for you are marked ⏸.

## Milestones

### M0: Spec close-out and baseline
- `SPEC.md`: move the three open questions into Decisions (the picks above) and set the status to Approved.
- Record a **baseline** from the unchanged site: for every page with a form (P1, P7–P15, P18–P20), each form's `action`, `method` and sorted input `name`s, captured with `javascript_tool`. Save it as `docs/ui-ux/baseline-forms.json`. Also take before-screenshots at 375 and 1440 px for the M8 report.
- Commit: `docs: approve UI redesign spec, record form baseline`.

### M1: `ui-brand` ⏸ (logo gate)
1. Draw the K + check symbol as SVG on a 24-unit grid, as `wwwroot/img/klasly-mark.svg`. Screenshot it at 16, 32, 48 and 128 px, on light and as the reverse.
2. **⏸ You approve or revise the symbol.** It isn't used anywhere until you do.
3. Make the remaining versions:
   - `klasly-logo.svg` (symbol + wordmark);
   - `klasly-logo-reverse.svg`;
   - `wwwroot/favicon.svg`;
   - delete the template `favicon.ico`.
4. Inter: download the v4.1 release zip into a new empty folder in the scratchpad. Copy `InterVariable.woff2` to `wwwroot/fonts/`, and the license to `wwwroot/fonts/OFL.txt`. `.gitignore` doesn't block either.
5. Android: replace `res/drawable/ic_launcher_foreground.xml` (the monogram, also used as the monochrome icon) and `ic_launcher_background.xml` (teal). Nothing changes in `mipmap-anydpi`, because minSdk 26 means the adaptive icon is always used. Note in the report that a teammate must run `./gradlew assembleDebug`.

### M2: `ui-foundation` ⏸ (touches every page)
- **`wwwroot/css/site.css`:**
  - `@font-face` for Inter;
  - the `:root` tokens from SPEC B1, mapped onto `--bs-*`;
  - `.btn-primary`, `.btn-outline-primary` and nav-pills variables;
  - the B3 type scale on the existing `h1.h3` / `h2.h5` / `h3.h6`, and 16 px base (the 14 px media query goes);
  - `:focus-visible` outline;
  - B4 spacing and panel rules;
  - `.form-control` / `.form-select` / `.form-check-input` borders in `#8696A7`;
  - remove the sticky-footer rules.
  - Keep `.qr-code`, `.cell-readable`, `.score-input` and `.overview-toggle`.
- **`Views/Shared/_Layout.cshtml`:**
  - skip link and `main#main`;
  - logo, using the inline symbol and an Inter wordmark;
  - Bootstrap `navbar-toggler` + `collapse` (Admin `navbar-expand-lg`, others `navbar-expand-sm`);
  - area-aware active state (Teacher `Classes`/`Sessions`/`Assessments` all highlight "My classes");
  - `ViewData["AlertsInPage"]` switch;
  - remove the footer and the empty import map;
  - add the `favicon.svg` link.
  - Keep the logout form, script order and `RenderSection`.
- **Delete `Views/Shared/_Layout.cshtml.css`.** If it's the only scoped CSS file, also remove the `Klasly.Web.styles.css` `<link>`; check for other `*.cshtml.css` files first.
- **`Views/Shared/_Alerts.cshtml`:** success alerts become dismissible (`role="status"`); errors stay `role="alert"`. This is handoff P3-9, pulled forward because the file is being edited anyway.
- **New `Views/Shared/_StatusBadge.cshtml`**, model `(string Text, string Variant)`, using subtle badges, with a filled variant for At risk.
- **`wwwroot/js/site.js`:** keep `data-confirm`, and add:
  - **pending submit:** disable the submitter and show `data-pending-text`, but only when the confirm passed and `$(form).valid()` is true (when jQuery Validate is loaded). Re-enable on `pageshow` so bfcache doesn't leave buttons dead;
  - **`data-warn-unsaved`** guard.
- **`DESIGN.md`** at the repo root, from SPEC A–B.
- Check every page once (layout regressions) at 320, 375, 768 and 1440 px, and measure contrast of body, muted, link, button and badge text on their real backgrounds.
- **⏸ You review the screenshots** before page work starts.

### M3: `ui-message-pages`
- `Program.cs:85`: `UseStatusCodePages()` becomes `UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}")`. The original status code (404 etc.) is kept.
- `Controllers/HomeController.cs`: new anonymous `StatusCode(int code)` action with `ResponseCache(NoStore)`. It returns the new `Views/Shared/NotFound.cshtml` for 404, and a generic message page otherwise.
- Rewrite `Views/Shared/Error.cshtml` (keep `ErrorViewModel` and the request id as "Reference"; remove the Development Mode section).
- `ApiUnavailable.cshtml`: user-facing copy and a real `<button type="button">`.
- `Views/Account/AccessDenied.cshtml`: copy that's correct for role refusals; render the API reason itself via `AlertsInPage`.
- All four pages share one message-page pattern.
- Verify: `/Admin/Classes/Details/999999` shows the styled page with HTTP 404; stopping the API shows ApiUnavailable.

### M4: `ui-teacher` ⏸ (core classroom flow)
- **Separate error messages (P1-5):**
  - Add a small `MoveSummaryErrorsTo(this ModelStateDictionary, string key)` to `Infrastructure/ModelStateExtensions.cs`, next to `AddApiErrors` (L12).
  - Call it in `SessionsController.Start` (after L32) with `"start"`, and in `AssessmentsController.Create` (after `AddFormErrors`, L29) with `"assessment"`.
  - `_StartSession.cshtml:9` shows only the `"start"` errors.
  - `_AssessmentForm.cshtml:6` shows `"assessment"` errors in create mode, and keeps `ModelOnly` in edit mode, which P20 uses.
- **Return to the corrected row (P3-3):**
  - `SessionsController.Correct` L66 becomes `RedirectToAction(nameof(Details), null, new { id }, $"row-{studentId}")`.
  - `_Live.cshtml` L62 gets `<tr id="row-@r.StudentId">`.
- **`Classes/Index`:** the class name is the link; Term, Schedule and Room hidden below `md`, with schedule and room shown under the name instead.
- **`Classes/Details`:**
  - live-session strip on top when a session `IsLive`, with the Start form collapsed behind "Start another session";
  - the weights line shows only when there are assessments, plus a "Weights complete" state;
  - a wider Add button;
  - status badges via `_StatusBadge` (`_SessionStatusBadge` keeps its logic and renders through it).
- **`Sessions/Details` + `_Live`:**
  - a large "N / M checked in" count;
  - the "Press End session to close it and correct results" hint in the ended-but-open state;
  - badges via `_StatusBadge`.
  - **Must not change:** the ids `live-panel` / `live-status` / `live-announcer`, the single root carrying `data-closed` / `data-summary`, the poller script and the QR box.
- **`Assessments/Details`:**
  - sticky Save bar with a live "N of M graded" count;
  - `data-warn-unsaved` on the grade form;
  - remove the phone-notification sentence (L26).
  - Keep the `for` loop, `Grades[i]` names and `asp-format`.
- Verify the full flow:
  - start a session (watch `Live` polls every ~5 s; double-click Start → one session);
  - stop the API to see "Reconnecting…", then end the session;
  - correct a row (the page lands on that row);
  - force an error on each class-page form (each shows once);
  - type grades, try to leave (warning), then save.
- **⏸ You review.**

### M5: `ui-parent`
- `Children/Index`: each whole list item is a `list-group-item-action` link named after the child. This fixes the label-in-name problem.
- `Children/Details`:
  - a one-line answer under the header;
  - `o.Classes.OrderByDescending(c => c.IsAtRisk == true)` at L47;
  - attendance as four labelled tabular numbers, with Absent emphasized;
  - risk badges via `_StatusBadge` (At risk filled; On track and Not computed subtle).
  - Keep the disclaimer, `aria-labelledby`, `.overview-toggle`, and the switcher shown only when there's more than one child.
- Verify at 375 px with a one-child parent and a multi-child parent.

### M6: `ui-admin`, in two commits
- **M6a, lists:**
  - Admin home: add Failed emails.
  - Terms: Active/Inactive on every row; a "Make active" confirm that names the term it replaces.
  - Subjects: row context on actions.
  - Accounts:
    - one "New account" dropdown;
    - one role order for the pills and the dropdown;
    - hide the Role column when a filter is on;
    - phones: hide Email and show it under the name instead;
    - row context on actions.
  - Classes: the name links to Details; the filter submits on change (the Show button stays for no-JS); the all-terms empty-state copy.
  - Class details: enroll form above the roster; active students first, dropped grouped.
  - Failed emails: "the email service" instead of Brevo (L10, L16); link to Accounts?role=Parent.
  - Everywhere: the row-action convention and `_StatusBadge`.
- **M6b, forms:**
  - Breadcrumbs and record names in Edit headings on the Terms, Subjects, Accounts and Classes forms.
  - Account form: fieldsets (Login, {Role} details, Status), with `data-warn-unsaved` on the main form.
  - `_ParentLinks`: its own panel with a "saved immediately" note; Link as `btn-outline-primary`.
  - Class form: Term field first; help text when the saved teacher is inactive.

### M7: `ui-auth-polish`
- `Views/Account/Login.cshtml`:
  - canvas background and a centered white panel;
  - logo + "Attendance and grades for your classes";
  - alerts inside the column (`AlertsInPage`);
  - "Logging in…" pending text.
  - Keep the field names, `ReturnUrl`, autocomplete attributes and validation.

### M8: Final QA and hand-off
- Full sweep of all 22 pages plus Not found:
  - 320, 375, 768 and 1440 px;
  - keyboard pass;
  - contrast measured;
  - Inter loading (`document.fonts.check`);
  - form-contract diff of every page against the M0 baseline.
- `/impeccable` finish review and `/review` of the whole branch diff; fix the findings.
- `dotnet build` / `dotnet test`. Tick the SPEC success criteria and write a summary report with before/after screenshots.
- After the demo, and only on your go-ahead: open a PR from `ui-redesign` into `dev`.

## Critical files
- `src/Klasly.Web/wwwroot/css/site.css`, `wwwroot/js/site.js`, `Views/Shared/_Layout.cshtml`, `_Alerts.cshtml`, new `_StatusBadge.cshtml` / `NotFound.cshtml`
- `src/Klasly.Web/Program.cs:85`, `Controllers/HomeController.cs`, `Infrastructure/ModelStateExtensions.cs:12`
- `Areas/Teacher/Controllers/SessionsController.cs` (Start L19–39, Correct L50–67), `AssessmentsController.cs` (Create L16–36)
- `Areas/Teacher/Views/**`, `Areas/Parent/Views/Children/*`, `Areas/Admin/Views/**`, `Views/Account/*`, `Views/Shared/Error.cshtml`, `ApiUnavailable.cshtml`
- `android/KlaslyApp/app/src/main/res/drawable/ic_launcher_{foreground,background}.xml`
- `SPEC.md`, `DESIGN.md`, `docs/ui-ux/baseline-forms.json`

## Reused, not rebuilt
- `data-confirm` in `site.js`.
- `ModelStateExtensions.AddApiErrors`.
- `ManilaTime`.
- `_SessionStatusBadge`'s status logic.
- The Bootstrap collapse JS, which is already loaded.
- The existing `.table-responsive` / `d-none d-md-table-cell` phone pattern.

## Verification (end to end)
- `dotnet build Klasly.slnx` with 0 warnings, and `dotnet test` all green, after every milestone.
- Browser checks with the preview tools on `api` + `web` at the widths listed above. The form-contract diff against `baseline-forms.json` is the main guard for routes, fields and anti-forgery tokens, since there are no Web UI tests.
- Live session end to end, as in M4.
- The SPEC.md success-criteria checklist, all ticked, in M8.

## Not covered
- No automated UI tests (Playwright would be a new package).
- The Android icon isn't built or checked on a device here.
- No merge or deploy before the demo.
