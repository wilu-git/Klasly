# Spec: Web UI/UX redesign (design system + all MVC pages)

> Status: **Draft — waiting for team approval** (2026-11-20).
> The previous spec (Demo deploy and demo data) is in git history (commit `00336a5`).
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` → `CLAUDE.md` → this spec → `docs/ui-ux/UI_UX_HANDOFF.md` (page-by-page detail, "Must preserve" lists).
> The handoff was checked against the code on 2026-11-20 (`_Layout.cshtml`, `site.css`, `site.js` match what it describes).

## Objective

Make the Klasly website (`src/Klasly.Web`) look and work like one carefully designed application instead of 22 separately styled Razor pages, **without changing what it does**.

**Users and their one job each**
| Role | Typical visit | What the UI must make fast |
|---|---|---|
| Teacher | At the front of a room, laptop or phone, under time pressure | Start a session, show the QR, see who checked in, fix a result, enter grades |
| Admin | Desk, laptop, many records | Find a record, change it, see that it worked |
| Parent | Phone, a few minutes | "Is my child OK?" answered at the top of the page |

**Design mode: Operate** (Impeccable). Refine the existing Bootstrap 5.3.3 look; the Teacher area is the style authority. No new visual identity, no gradients, glass, hero sections, KPI tiles, decorative icons or heavy shadows.

**Success looks like:** every page uses the same tokens, headings, buttons, badges, tables and message patterns; navigation never wraps or overflows on a 320 px phone; the three critical problems (template Error page, buried live session, no double-submit protection) are gone; every form posts exactly the same fields as before.

## Capability map

The request bundles independently testable pieces. One module per change set, built in this order. Module ids are fixed.

| Module id | Responsibility | Handoff items | Depends on |
|---|---|---|---|
| `ui-foundation` | Tokens, type, spacing, layout/nav, alerts placement, shared JS helpers, `_StatusBadge`, `DESIGN.md` | P2-1, P2-2, P2-3, P1-3, §7.4 (1, 3, 6, 7) | — |
| `ui-message-pages` | Error, Not found, Access denied, Server unavailable on one pattern | P1-1, P1-2 *(ask)*, P2-4 | `ui-foundation` |
| `ui-teacher` | My classes, class page, session page, grade sheet | P1-4, P1-5 *(ask)*, P1-6, P2-13 (teacher), P2-14, P3-3 *(ask)*, P3-4 | `ui-foundation` |
| `ui-parent` | My children, child overview | P2-9, P2-10 | `ui-foundation` |
| `ui-admin` | Admin home, terms, subjects, accounts, classes, enrollments, failed emails | P2-5–P2-8, P2-11, P2-12, P2-13 (admin), P3-5–P3-8 | `ui-foundation` |
| `ui-auth-polish` | Login brand moment, favicon, dismissible alerts | P3-1, P3-2, P3-9 | `ui-foundation` |

Build order: `ui-foundation` → `ui-message-pages` → `ui-teacher` → `ui-parent` → `ui-admin` → `ui-auth-polish`.
(`ui-teacher`, `ui-parent` and `ui-admin` don't touch each other's files and could run in parallel after the foundation.)

---

## A. Global design system (`ui-foundation`)

All values live as CSS custom properties at the top of `wwwroot/css/site.css`. Views use Bootstrap classes and these tokens only — **no hex colors in `.cshtml` files**.

### A1. Color tokens

One brand hue, used for actions and links only. Everything else is neutral or semantic. Contrast is against `#fff`.

| Token | Value | Use | Contrast |
|---|---|---|---|
| `--k-primary` → `--bs-primary` | `#1f5fbf` | Primary buttons, links, active nav, focus ring | 6.1 : 1 |
| `--k-primary-hover` | `#184c99` | Hover/active of primary | 8.3 : 1 |
| `--k-primary-subtle` | `#e8eff9` | Active nav background, selected pill | — |
| `--k-bg` (body) | `#f6f7f9` | Page background (very light grey, so white panels read as surfaces) | — |
| `--k-surface` | `#ffffff` | Navbar, tables, cards, forms panels | — |
| `--k-text` → `--bs-body-color` | `#1d2430` | Body text | 15 : 1 |
| `--k-text-muted` → `--bs-secondary-color` | `#5b6472` | Meta lines, help text, empty states | 5.9 : 1 |
| `--k-border` → `--bs-border-color` | `#dde1e7` | Table rules, dividers, inputs | — |
| `--k-focus` | `rgba(31,95,191,.45)` | 3 px focus ring on every interactive element | — |
| Success / Warning / Danger / Info | Bootstrap 5.3 defaults (`--bs-success` etc.) and their `-subtle` / `-emphasis` pairs | Status only | emphasis text ≥ 4.5 : 1 |

Secondary color: none. A second accent would compete with status colors; neutrals do that job.
Disabled: Bootstrap's `opacity: .65` + `cursor: not-allowed`; never the only signal (button text changes too, see A5).
Template colors (`#0077cc`, `#1b6ec2`, `#258cfb`) and `_Layout.cshtml.css` are deleted.

Because Bootstrap compiles button colors, `.btn-primary` / `.btn-outline-primary` also get their `--bs-btn-*` variables set once in `site.css`.

### A2. Status semantics (one mapping, everywhere)

Every status shows **text**, never color alone. Default style is the calm *subtle* badge (`bg-*-subtle text-*-emphasis border border-*-subtle`); only "At risk" is a filled badge, because it is the one thing a parent must not miss.

| Meaning | Variant | Statuses |
|---|---|---|
| Good / done | success | Active (term, account, enrollment), Present, Open (session), On track |
| Needs a look | warning | Late, weights under 100 % |
| Bad / destructive | danger | Absent, Cancelled, **At risk** (filled `text-bg-danger`) |
| Neutral info | info | Excused, Scheduled |
| Inactive / finished | secondary | Inactive, Dropped, Ended, Closed, Finalized, Not computed yet, Not yet (scanned) |

Implemented by one partial `Views/Shared/_StatusBadge.cshtml` (model: text + variant). `_SessionStatusBadge`, the attendance badge in `_Live`, and the risk badge keep their own status→text logic but render through it.

### A3. Typography

System font stack (Bootstrap's `system-ui` stack). No web font: nothing to download on a school network, and it's legible.

| Role | Markup (unchanged convention) | Size / weight / line-height |
|---|---|---|
| Page title | `h1.h3` | 1.5rem (24 px) / 600 / 1.25 — fixed, not Bootstrap's fluid `calc()` |
| Section heading | `h2.h5` | 1.125rem (18 px) / 600 / 1.3 |
| Sub-section | `h3.h6` | 1rem / 600 / 1.4 |
| Body, inputs, table cells | — | 1rem (16 px) / 400 / 1.5 — **at every width** (removes the 14 px phone rule) |
| Supporting / meta / help | `.small`, `.form-text` | 0.875rem / 400, muted color |
| Form labels | `.form-label` | 0.9375rem / 500 |
| Table headers | `thead th` | 0.8125rem / 600, muted, no uppercase |
| Buttons | `.btn` | 1rem / 500 (`.btn-sm` 0.875rem) |
| Numbers | `.table`, `.num` | `font-variant-numeric: tabular-nums` (scores, counts, student numbers, times) |

The existing heading classes stay, so no view needs heading markup changes; only their sizes are tuned in CSS.

### A4. Spacing and layout

Bootstrap's spacer scale only (`1`=4 px, `2`=8, `3`=16, `4`=24, `5`=48). No arbitrary values in views.

| Rule | Value |
|---|---|
| Content container | `.container`, capped at 1140 px (Bootstrap's 1320 px `xxl` width is too wide for tables and forms) |
| Mobile gutter | 16 px (Bootstrap `.container` default 12 px raised via `--bs-gutter-x: 2rem`) |
| Navbar → page content | `mt-4` |
| Breadcrumb → title | `mb-2` |
| Page header → first content | `mb-4` |
| Between sections | `mt-5` |
| Form field groups | `mb-3`; fieldset gap `mb-4` |
| Form width | `col-12 col-md-8 col-lg-6` (unchanged) |
| Table cells | Bootstrap default (`.5rem`); `table-sm` only on the dense teacher roster/grade sheet |
| Panels (cards) | `p-3` mobile, `p-4` ≥ md; 1 px border, radius `.5rem`, **no shadow** |

### A5. Components

| Component | Standard |
|---|---|
| Primary button | `btn-primary`; **one per page region** (page header or form) |
| Secondary button | `btn-outline-secondary` — all non-destructive secondary actions, page and row level |
| Destructive, row | `btn btn-sm btn-link link-danger`, always last in the row, always `data-confirm` |
| Destructive, page | `btn-outline-danger` (End session only) with `data-confirm` |
| Inline add (Enroll, Link) | `btn-outline-primary` in a `form.row.g-2.align-items-end` |
| Row's main destination | The record name cell is the link (class, child, account) |
| Repeated row actions | Visually hidden row context: `Edit<span class="visually-hidden"> {name}</span>` |
| Forms | Visible labels always; help text `.form-text`; `asp-validation-summary="ModelOnly"` at top; Save (primary) + Cancel (outline secondary), left aligned |
| Tables | `.table-responsive > table.table.align-middle` on a white surface; `th scope="col"`; hidden "Actions" header; actions `text-end text-nowrap` |
| Breadcrumbs | On every page below a list (detail **and** form pages): `List › Record › Sub-page`; last item `aria-current="page"` |
| Page header | breadcrumb → `h1.h3` + optional meta line (`p.text-body-secondary`) → primary action right (stacks under the title below `sm`) |
| Alerts (flash) | Inside `main`, aligned with page content; success `role="status"` + dismiss button; error `role="alert"`, not dismissible |
| Empty state | One muted sentence that names the next step; a link/button to it if one exists |
| Confirmation | Native `confirm()` via existing `data-confirm` (kept: accessible, zero code) |
| Pending submit | `site.js`: after a **valid**, **confirmed** submit, disable the submitter and swap to `data-pending-text` (default "Saving…") |
| Unsaved guard | `site.js`: `<form data-warn-unsaved>` warns on `beforeunload` when dirty; cleared on submit |
| Message page | Heading, one sentence, one primary action, optional small "Reference: …" — used by Error, Not found, Access denied, Server unavailable |

### A6. Breakpoints (Bootstrap's own)

| Band | Width | Behavior |
|---|---|---|
| Mobile | 320–575 (`xs`) | Single column; nav collapsed behind a toggler; page-header actions stack under the title; secondary table columns hidden and their key info shown under the name in small muted text; full-width form buttons |
| Tablet | 576–991 (`sm`, `md`) | Forms keep side-by-side pairs from `sm`; Admin nav still collapsed (5 items); Teacher/Parent nav expanded; teacher session page stacks QR above roster |
| Desktop | ≥ 992 (`lg`) | Full nav; session page QR left / roster right; tables show all columns |

Horizontal scrolling is allowed **only** inside `.table-responsive`, and only for the failed-emails table and the teacher grade sheet, where every column is needed.

### A7. `DESIGN.md`

Write `DESIGN.md` at the repo root from sections A1–A6 (Impeccable's design file), so later `/impeccable` runs have a direction to check against.

---

## B. Application layout (`ui-foundation`)

`Views/Shared/_Layout.cshtml`, top to bottom:

1. Visually hidden **"Skip to content"** link → `<main id="main">`.
2. **Navbar** on a white surface with a bottom border (no shadow):
   - Brand: wordmark "Klasly" in `--k-primary`, 600 weight → `/` (routes by role, unchanged).
   - Role nav, collapsing with Bootstrap's `navbar-toggler` + `collapse` (`aria-controls`, `aria-expanded`, label "Menu"). Admin collapses below `lg` (5 items); Teacher and Parent below `sm`.
   - Active item: `--k-primary` text + `--k-primary-subtle` background + `aria-current="page"`. **Area-aware**: in the Teacher area, `Classes`, `Sessions` and `Assessments` all highlight "My classes".
   - Right side: user's name (muted, small) + "Log out" (`btn-sm btn-outline-secondary`, unchanged POST form). On phones they sit inside the collapsed menu.
3. `main#main.container` with `<partial name="_Alerts" />` then `@RenderBody()`. A page can set `ViewData["AlertsInPage"] = true` and render `_Alerts` itself (Login, Access denied) so alerts line up with a narrow column.
4. **Footer removed** (it said only "© 2026 Klasly" and took a fixed 60 px band); the absolute-position rules go with it.
5. Scripts unchanged in order: jQuery → Bootstrap bundle → `site.js` → `Scripts` section. Empty import map removed.

**Role navigation**

| Role | Items | Why |
|---|---|---|
| Admin | Terms · Subjects · Accounts · Classes · Failed emails | Setup order; Failed emails last (exception queue) |
| Teacher | My classes | Every teacher task starts from a class; the class page is the hub |
| Parent | My children | One destination; the child switcher on the overview handles several children |

A sidebar was considered for Admin and rejected: 5 items fit one row at `lg`, and a sidebar adds markup and a second responsive pattern for no task gain.

## C. Page patterns and integration

Page-level detail and every "Must preserve" list are in `docs/ui-ux/UI_UX_HANDOFF.md` §5. This spec fixes the patterns they follow.

| Pattern | Pages | Structure |
|---|---|---|
| List | P7, P9, P11, P13, P16, P17, P21 | Header (title + one primary action) → filter (if any) → table or empty state |
| Form | P8, P10, P12, P14 | Breadcrumb → title (Edit names the record) → form (fieldsets on P12) → Save/Cancel |
| Detail hub | P15, P18 | Breadcrumb → header + meta + secondary action → main task first → supporting lists |
| Live operation | P19 | Header + status + End session → QR (≥ 320 px, white, quiet zone) + big checked-in count + roster |
| Data entry | P20 | Breadcrumb → header → grade table → sticky Save bar with live "N of M graded" → Edit assessment (`<details>`) |
| Monitoring | P22 | Header → one-line answer ("All classes on track" / "1 of 4 classes needs attention") → one panel per class |
| Message | P2, P4, P5, Not found | A5 message page |

**Flows that must read as one path** (breadcrumbs + the same names on both ends):
- Admin: Classes → *{code} – {section}* (enroll above roster, dropped grouped) → Edit class.
- Admin: Accounts (*one* "New account" dropdown, filter pills in the same order) → New/Edit {role} → Linked students ("saved immediately" note, separate panel).
- Teacher: My classes (class name is the link) → class page (**live session strip on top** when one is open; Start form collapsed behind "Start another session" while live) → session page.
- Teacher: class page `#assessments` → grade sheet → breadcrumb back to `#assessments`.
- Parent: My children (whole row is the link) → child overview (switcher pills when > 1 child).

## Tech stack (unchanged)

ASP.NET Core 10 MVC, Razor views, Bootstrap 5.3.3 (local `wwwroot/lib`), jQuery + jQuery Validation Unobtrusive, QRCoder (server-side SVG). **No new packages, no icon library, no web font, no front-end framework.**

## Commands

```powershell
dotnet build Klasly.slnx
dotnet test Klasly.slnx
```
Browser check: `preview_start` the `api` then `web` configurations (`.claude/launch.json` → https://localhost:7256, https://localhost:7144), log in with `docs/TEST-ACCOUNTS.md`.
After code changes: `graphify update .`

## Project structure (files this work touches)

```
src/Klasly.Web/
├── wwwroot/css/site.css               ← tokens + all shared component CSS (one file)
├── wwwroot/js/site.js                 ← data-confirm (kept) + pending submit + unsaved guard
├── wwwroot/favicon.ico                ← ui-auth-polish
├── Views/Shared/_Layout.cshtml        ← nav, skip link, alerts slot, no footer
├── Views/Shared/_Layout.cshtml.css    ← deleted (dead template CSS)
├── Views/Shared/_Alerts.cshtml        ← dismissible success
├── Views/Shared/_StatusBadge.cshtml   ← new: one badge for every status
├── Views/Shared/Error.cshtml, ApiUnavailable.cshtml, NotFound.cshtml (new, ask)
├── Views/Account/*.cshtml
└── Areas/{Admin,Teacher,Parent}/Views/**   ← page work per module
DESIGN.md                              ← new: Impeccable design file
docs/reports/<timestamp>-ui-<module>/  ← CLAUDE.md §5 reports, one folder per module
```

## Code style

```css
/* site.css — tokens first, then Bootstrap overrides, then Klasly components. */
:root {
  --k-primary: #1f5fbf;
  --k-primary-hover: #184c99;
  --bs-primary: var(--k-primary);
  --bs-primary-rgb: 31, 95, 191;
  --bs-link-color-rgb: 31, 95, 191;
  --bs-body-bg: #f6f7f9;
  --bs-body-color: #1d2430;
  --bs-secondary-color: #5b6472;
  --bs-border-color: #dde1e7;
}
.btn-primary {
  --bs-btn-bg: var(--k-primary);
  --bs-btn-border-color: var(--k-primary);
  --bs-btn-hover-bg: var(--k-primary-hover);
  --bs-btn-hover-border-color: var(--k-primary-hover);
}
```

```cshtml
@* Row actions: name is the link, secondary outline, destructive last + confirmed, row context for screen readers. *@
<td><a asp-action="Details" asp-route-id="@c.ClassId" class="fw-semibold">@c.SubjectCode – @c.Section</a></td>
<td class="text-end text-nowrap">
    <a asp-action="Edit" asp-route-id="@c.ClassId" class="btn btn-sm btn-outline-secondary">Edit<span class="visually-hidden"> @c.SubjectCode @c.Section</span></a>
</td>
```

- Presentation only in views: no business rules, no thresholds, no hard-coded 20 % / 75.
- Comment only the *why* a CSS rule exists (one line), as `site.css` already does.

## Testing strategy

There are no automated UI tests for `Klasly.Web` and this work adds none (Playwright would be a new package — out of scope unless asked). Verification per module:

1. `dotnet build` (0 warnings) and `dotnet test` (all green) — proves nothing server-side broke.
2. **Form-contract check:** before changing a page, record each form's `action`, `method` and the sorted list of input `name`s with `javascript_tool`; after the change, compare. They must match exactly (except where this spec approves a change).
3. **Anti-forgery check:** every `form[method=post]` contains `__RequestVerificationToken`.
4. **Responsive check** at 320, 375, 768 and 1280 px: `document.documentElement.scrollWidth <= clientWidth`; nav one row; screenshots for the report.
5. **Flow check** in the browser: each form's success and error path; session page — start, watch the 5 s `Live` polls in network requests, stop the API to see "Reconnecting…", end, correct a row; grade sheet — type, try to leave (warning), save.
6. Keyboard: tab through nav, toggler, forms; visible focus everywhere. Console: no errors.
7. `/impeccable` detect on changed views: no findings.

## Boundaries

- **Always:** keep routes, POST methods, anti-forgery, field names, validation, `[Authorize]`, the live-session contract (`live-panel`, `live-status`, `live-announcer`, single-root `_Live` with `data-closed`/`data-summary`) and QR readability (handoff §8); verify in a real browser before claiming a visual result; write the CLAUDE.md §5 reports per module.
- **Ask first:** any controller or `Program.cs` change; any new API call; any feature from the handoff's "Optional" list; changing user-facing wording about Brevo or push notifications beyond what Open Questions settle.
- **Never:** add packages, frameworks, icon fonts or web fonts; put colors in views; fork `_AssessmentForm` or `_Live`; add client-side business rules; deploy to klasly.site or merge to `master` as part of this work; commit unless asked (no Claude attribution).

## Success criteria

- [ ] `site.css` defines the A1 tokens; `grep -r "#[0-9a-fA-F]\{3,6\}" Views Areas` finds no colors in views; `_Layout.cshtml.css` is gone.
- [ ] Base font is 16 px at 320 px and at 1280 px.
- [ ] At 320/375/768/1280 px, no page scrolls sideways except inside the two allowed tables; the navbar is one row with a working toggler where A6 says it collapses.
- [ ] "My classes" is highlighted on every `/Teacher/*` page.
- [ ] Every status in A2 renders through `_StatusBadge` with the listed variant.
- [ ] Form-contract and anti-forgery checks pass on every page with a form (P1, P7–P15, P18–P20).
- [ ] A valid submit disables its button with pending text; a cancelled confirm or a failed client validation does not.
- [ ] Leaving the grade sheet with typed, unsaved scores warns; saving does not.
- [ ] Teacher class page with an open session shows the live strip with "Show QR" above everything else.
- [ ] The two forms on the teacher class page never show the same error twice *(if P1-5 approved)*.
- [ ] Error page has no "Development Mode" text; a bad id (e.g. `/Admin/Classes/Details/999999`) shows a styled Not found page with navigation *(if P1-2 approved)*.
- [ ] Session page at 375 px and 1280 px: QR ≥ 320 px wide on white; `Live` polled every ~5 s; reconnect warning appears when the API stops.
- [ ] Parent overview shows a one-line answer above the class panels.
- [ ] `dotnet build` 0 warnings, `dotnet test` all green, `/impeccable` detect clean, reports written for each module.

## Open questions

1. **Timing vs. the demo on 2026-11-21.** klasly.site is live with demo data and the demo is tomorrow. Proposal: do this on a branch `ui-redesign` off `dev`, and don't merge or deploy until after the demo. Or do you want some modules (e.g. `ui-foundation` + `ui-message-pages` + `ui-teacher`) live for the demo?
2. **The three "(ask)" controller/Program changes** — approve all, some, none?
   - P1-2 styled Not found page (`Program.cs` `UseStatusCodePagesWithReExecute` + `HomeController.StatusCode` + view).
   - P1-5 form-specific error keys in `SessionsController.Start` / `AssessmentsController.Create` (stops the duplicate error).
   - P3-3 return to the corrected row (`RedirectToAction(..., fragment: "row-{studentId}")` in `SessionsController.Correct`).
3. **Brand color** `#1f5fbf` (calm academic blue, 6.1 : 1 on white) and light-grey page background `#f6f7f9` — OK, or does the team have a color/logo?
4. **Impeccable files:** does the team have a `PRODUCT.md` / `DESIGN.md` outside the repo? If not, I'll write `DESIGN.md` from section A.
5. **Parent overview:** put at-risk classes first? (Changes the order parents see.)
6. **Grade sheet sentence** "A student's phone is notified when their first grade is saved." — Firebase push is deferred (commit `9615d2b`), so it's untrue today. Remove it?
7. **Footer:** OK to remove it entirely?
8. **Failed emails:** say "the email service" instead of "Brevo" in the page text?

## Out of scope

Android app; API changes; new features from the handoff's Optional list (full-screen QR, account search, searchable pickers, active-term status on Admin home, list-page live/at-risk indicators, dark mode) unless asked.
