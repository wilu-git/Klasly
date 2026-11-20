# Spec: Web UI/UX redesign (brand, design system, all MVC pages)

> Status: **Approved** (2026-11-20). Revision 4: the team's brand direction (Klasly Teal, Inter, K + check logo) replaces the first draft's color and type choices; all three open questions are answered under Decisions 8–10.
> The previous spec (Demo deploy and demo data) is in git history (commit `00336a5`).
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` → `CLAUDE.md` → this spec → `docs/ui-ux/UI_UX_HANDOFF.md` (page-by-page detail, "Must preserve" lists).
> The handoff was checked against the code on 2026-11-20 (`_Layout.cshtml`, `site.css`, `site.js` match what it describes).

## Objective

Make the Klasly website (`src/Klasly.Web`) look and work like one carefully designed application instead of 22 separately styled Razor pages, with a real brand identity, **without changing what it does**.

**Brand personality:** professional, trustworthy, approachable, organized, efficient. Dependable education technology, not a children's learning app and not a marketing site.

**Users and their one job each**
| Role | Typical visit | What the UI must make fast | Brand treatment |
|---|---|---|---|
| Teacher | Front of a room, laptop or phone, under time pressure | Start a session, show the QR, see who checked in, fix a result, enter grades | Fast classroom workflow, clear live-session states |
| Admin | Desk, laptop, many records | Find a record, change it, see that it worked | Dense, structured tables; restrained teal actions |
| Parent | Phone, a few minutes | "Is my child OK?" answered at the top of the page | Friendly, readable summaries; clear attendance status |
| Everyone | Login | Get in | Logo, teal primary action, quiet neutral background |

**Design mode: Operate** (Impeccable). Refine the existing Bootstrap 5.3.3 system; the Teacher area is the house-style reference. Restrained borders, consistent spacing, modest radii. No gradients, glass, hero sections, card grids, decorative dashboards, excess icons or oversized rounded components.

**Success looks like:** every page uses the same tokens, type scale, buttons, badges, tables and message patterns; the logo and teal identity appear consistently; navigation never wraps or overflows on a 320 px phone; the three critical problems (template Error page, buried live session, no double-submit protection) are gone; every form posts exactly the same fields as before.

## Capability map

One module per change set, built in this order. Module ids are fixed.

| Module id | Responsibility | Handoff items | Depends on |
|---|---|---|---|
| `ui-brand` | Logo SVGs (3 variants), favicon, bundled Inter font | P3-2 | — |
| `ui-foundation` | Tokens, type scale, spacing, layout/nav, alerts placement, shared JS helpers, `_StatusBadge`, `DESIGN.md` | P2-1, P2-2, P2-3, P1-3, §7.4 (1, 3, 6, 7) | `ui-brand` |
| `ui-message-pages` | Error, Not found, Access denied, Server unavailable on one pattern | P1-1, P1-2, P2-4 | `ui-foundation` |
| `ui-teacher` | My classes, class page, session page, grade sheet | P1-4, P1-5, P1-6, P2-13 (teacher), P2-14, P3-3, P3-4 | `ui-foundation` |
| `ui-parent` | My children, child overview | P2-9, P2-10 | `ui-foundation` |
| `ui-admin` | Admin home, terms, subjects, accounts, classes, enrollments, failed emails | P2-5–P2-8, P2-11, P2-12, P2-13 (admin), P3-5–P3-8 | `ui-foundation` |
| `ui-auth-polish` | Login brand moment, dismissible alerts | P3-1, P3-9 | `ui-foundation` |

Build order: `ui-brand` → `ui-foundation` → `ui-message-pages` → `ui-teacher` → `ui-parent` → `ui-admin` → `ui-auth-polish`.
(`ui-teacher`, `ui-parent` and `ui-admin` don't touch each other's files and could run in parallel after the foundation.)

---

## A. Brand identity (`ui-brand`)

### A0. Logo — Concept A: K + Check

A minimal geometric **K monogram whose lower leg is a checkmark**: K = Klasly, check = attendance and verification, geometric construction = organization.

- Construction: on a 24-unit grid, a vertical stem plus two diagonal strokes; the lower diagonal bends into a short check tick. Uniform stroke weight, square or slightly rounded caps, no outlines, no gradients, no graduation caps.
- Must read at 16 px (favicon) — tested by rendering the symbol at 16, 32 and 48 px.
- Teal `#126B64` on light backgrounds; white on navy/teal for the reverse.

| Variant | File | Use |
|---|---|---|
| Horizontal (symbol + "Klasly" wordmark) | `wwwroot/img/klasly-logo.svg` | Docs, reports, Android splash reference |
| Symbol only | `wwwroot/img/klasly-mark.svg` (+ `wwwroot/favicon.svg`) | Favicon, navbar on phones, Android app icon source |
| Reverse (white) | `wwwroot/img/klasly-logo-reverse.svg` | Dark backgrounds |

In the web navbar and login the logo is the **inline symbol SVG + the word "Klasly" as HTML text in Inter 600**, so it stays sharp, themeable and needs no text-to-path conversion. The standalone horizontal SVG uses `<text>` with the Inter/system stack.

**Gate:** the symbol is shown to the team as a screenshot (16/32/48/128 px, light and reverse) and approved **before** it is used anywhere.

### A0.1 Font: Inter, bundled locally

- `wwwroot/fonts/InterVariable.woff2`: the upright variable font only (~340 KB, cached once; the UI uses no italics), plus `wwwroot/fonts/OFL.txt` (the SIL Open Font License, which must ship with the font).
- `@font-face { font-family: "Inter"; font-display: swap; font-weight: 100 900; }` in `site.css`, fallback `system-ui, -apple-system, "Segoe UI", Roboto, sans-serif`.
- No CDN or Google Fonts call: the app works offline on campus networks and doesn't send visitors to a third party.
- This is a static asset, not a NuGet/npm package.

---

## B. Global design system (`ui-foundation`)

All values live as CSS custom properties at the top of `wwwroot/css/site.css`, mapped onto Bootstrap's variables. Views use Bootstrap classes and these tokens only — **no hex colors in `.cshtml` files**.

### B1. Color tokens

Contrast ratios are calculated (WCAG 2.x relative luminance), to be re-checked with a contrast tool during `ui-foundation`.

| Token | Value | Use | Contrast |
|---|---|---|---|
| `--k-teal` → `--bs-primary` | `#126B64` Klasly Teal | Primary buttons, links, logo, active nav text, focus ring | 6.3 : 1 on white · 5.9 on canvas · white text on teal 6.3 |
| `--k-teal-hover` | `#0E5650` | Hover / pressed primary | 8.1 : 1 on white |
| `--k-navy` → `--bs-heading-color`, `--bs-emphasis-color` | `#17324D` Deep Navy | Headings, strong text, nav links | 13.1 : 1 |
| `--k-teal-subtle` | `#E8F3F1` Pale Teal | Selected nav item, selected pill, live-session strip background | teal text on it 5.6 : 1 |
| `--k-canvas` → `--bs-body-bg` | `#F5F7F8` | Page background | — |
| `--k-surface` | `#FFFFFF` | Navbar, forms, tables, panels | — |
| `--k-text` → `--bs-body-color` | `#263746` | Body text | 12.2 : 1 |
| `--k-text-muted` → `--bs-secondary-color` | **`#5A6A80`** *(approved, Decision 8)* | Meta lines, help text, empty states | 5.5 on white · 5.1 on canvas |
| `--k-border` → `--bs-border-color` | `#DCE3E8` | Table rules, dividers, panel edges | decorative |
| `--k-border-control` *(approved, Decision 8)* | `#8696A7` | Input, select and checkbox outlines | 3.0 : 1 (WCAG 1.4.11) |
| Success / Warning / Danger / Info / Secondary | Bootstrap 5.3 defaults and their `-subtle` / `-emphasis` pairs | **Statuses only** — never brand decoration | emphasis text ≥ 4.5 : 1 |

> **Contrast note (approved, Decision 8):**
> - `#64748B` gives 4.8 : 1 on white but **4.4 : 1 on the `#F5F7F8` canvas**. That fails AA for the 13–14 px supporting text, and empty states and meta lines sit on the canvas. Proposal: `#5A6A80`, same slate hue, passes on both. The alternative is to keep `#64748B` and allow muted text only on white surfaces, which is hard to enforce.
> - `#DCE3E8` is fine for dividers but is only 1.3 : 1 as an **input outline**, and WCAG 1.4.11 needs 3 : 1 for control boundaries. Proposal: `#8696A7` for form controls only.

Usage rules: teal for primary actions, links and brand only; navy for headings and strong text; white for content surfaces; pale teal for selection and context highlights; red/yellow/green only for real statuses. **No teal badges, no teal section backgrounds.**

Bootstrap compiles button colors, so `.btn-primary`, `.btn-outline-primary`, `.link-primary` and `.nav-pills` also get their `--bs-btn-*` / `--bs-nav-*` variables set once in `site.css`.
Focus: `:focus-visible { outline: 2px solid var(--k-teal); outline-offset: 2px; }` replaces the template's `#258cfb` box-shadow ring (solid, ≥ 3 : 1, survives Windows high-contrast).
Disabled: Bootstrap's `opacity: .65` + `cursor: not-allowed`, and the button text changes too (B5), so it isn't color alone.
Template colors (`#0077cc`, `#1b6ec2`, `#258cfb`) and `_Layout.cshtml.css` are deleted.

### B2. Status semantics (one mapping, everywhere)

Every status shows **text**, never color alone. The default style is the calm *subtle* badge (`bg-*-subtle text-*-emphasis border border-*-subtle`). Only "At risk" is a filled badge, because it's the one thing a parent must not miss.

| Meaning | Variant | Statuses |
|---|---|---|
| Present / success | success | Active (term, account, enrollment), Present, Open (session), On track |
| Late / warning | warning | Late, weights under 100 % |
| Absent / error | danger | Absent, Cancelled, **At risk** (filled `text-bg-danger`) |
| Information | info | Excused, Scheduled |
| Inactive / neutral | secondary | Inactive, Dropped, Ended, Closed, Finalized, Not computed yet, Not yet (scanned) |

Implemented by one partial, `Views/Shared/_StatusBadge.cshtml` (model: text + variant). `_SessionStatusBadge`, the attendance badge in `_Live` and the risk badge keep their own status→text logic but render through it.

### B3. Typography (Inter → system fallback)

| Role | Markup (unchanged convention) | Size | Weight / line-height / color |
|---|---|---|---|
| Page title | `h1.h3` | **28 px** (1.75rem) below `lg`, **32 px** (2rem) at ≥ `lg` | 600 / 1.2 / navy |
| Section title | `h2.h5` | 20 px (1.25rem) | 600 / 1.3 / navy |
| Subsection | `h3.h6` | 16 px (1rem) | 600 / 1.4 / navy |
| Body, inputs | — | 16 px **at every width** (removes the 14 px phone rule) | 400 / 1.5 / text |
| Tables | `.table` | 15 px (0.9375rem) | 400 / 1.4, tabular numerals |
| Table headers | `thead th` | 14 px | 600, muted, no uppercase |
| Supporting / meta / help | `.small`, `.form-text` | 14 px (0.875rem) — 13 px only for the QR caption | 400, muted |
| Form labels | `.form-label` | 15 px | 500, text |
| Buttons | `.btn` | 15 px (`.btn-sm` 14 px) | 600 |
| Numbers | `.table`, `.num` | — | `font-variant-numeric: tabular-nums` (percentages, grades, dates, times, student numbers) |

The existing heading classes stay, so no view needs heading markup changes; only their sizes are set in CSS. Inputs stay at 16 px so iOS doesn't zoom on focus.

### B4. Spacing and layout

Bootstrap's spacer scale only (`1`=4 px, `2`=8, `3`=16, `4`=24, `5`=48). No arbitrary values in views.

| Rule | Value |
|---|---|
| Content container | `.container`, capped at 1200 px (Bootstrap's 1320 px `xxl` width is too wide for tables and forms on a 1440 px screen) |
| Mobile gutter | 16 px (`--bs-gutter-x: 2rem` on `.container`) |
| Navbar → page content | `mt-4` |
| Breadcrumb → title | `mb-2` |
| Page header → first content | `mb-4` |
| Between sections | `mt-5` |
| Form field groups | `mb-3`; fieldset gap `mb-4` |
| Form width | `col-12 col-md-8 col-lg-6` (unchanged) |
| Table cells | Bootstrap default; `table-sm` only on the teacher roster and grade sheet |
| Panels | white, 1 px `--k-border`, radius 8 px (`--bs-border-radius-lg`), `p-3` mobile / `p-4` ≥ md, **no shadow** |
| Radii | inputs and buttons 6 px (Bootstrap default), panels 8 px, badges 4 px. Nothing pill-shaped except nav pills. |

### B5. Components

| Component | Standard |
|---|---|
| Primary button | `btn-primary` (teal); **one per page region** (page header or form) |
| Secondary button | `btn-outline-secondary`, used for all non-destructive secondary actions at page and row level |
| Destructive, row | `btn btn-sm btn-link link-danger`, always last in the row, always `data-confirm` |
| Destructive, page | `btn-outline-danger` (End session only) with `data-confirm` |
| Inline add (Enroll, Link) | `btn-outline-primary` in a `form.row.g-2.align-items-end` |
| Row's main destination | The record name cell is the link (class, child, account) |
| Repeated row actions | Visually hidden row context: `Edit<span class="visually-hidden"> {name}</span>` |
| Forms | Visible labels always; help text `.form-text`; `asp-validation-summary="ModelOnly"` at top; Save (primary) + Cancel (outline secondary), left aligned, full width below `sm` |
| Tables | `.table-responsive > table.table.align-middle` on a white surface; `th scope="col"`; hidden "Actions" header; actions `text-end text-nowrap` |
| Breadcrumbs | On every page below a list (detail **and** form pages): `List › Record › Sub-page`; last item `aria-current="page"` |
| Page header | breadcrumb → `h1.h3` + optional meta line → primary action on the right (stacks under the title below `sm`) |
| Alerts (flash) | Inside `main`, aligned with page content; success `role="status"` + dismiss button; error `role="alert"`, not dismissible |
| Empty state | One muted sentence that names the next step, with a link or button to it if one exists |
| Confirmation | Native `confirm()` via the existing `data-confirm` (kept: accessible, zero code) |
| Pending submit | `site.js`: after a **valid**, **confirmed** submit, disable the submitter and swap to `data-pending-text` (default "Saving…") |
| Unsaved guard | `site.js`: `<form data-warn-unsaved>` warns on `beforeunload` when dirty; cleared on submit |
| Message page | Heading, one sentence, one primary action, optional small "Reference: …". Used by Error, Not found, Access denied, Server unavailable |
| Live-session strip | Pale-teal panel: "Session in progress · ends 10:30 AM" + teal "Show QR" button |

### B6. Breakpoints (Bootstrap's own)

| Band | Width | Behavior |
|---|---|---|
| Mobile | 320–575 (`xs`) | Single column; nav collapsed behind a toggler, symbol-only logo; page-header actions stack under the title; secondary table columns hidden, with their key info shown under the name in small muted text; full-width form buttons |
| Tablet | 576–991 (`sm`, `md`) | Forms keep side-by-side pairs from `sm`; Admin nav still collapsed (5 items); Teacher/Parent nav expanded; the teacher session page stacks the QR above the roster |
| Desktop | ≥ 992 (`lg`) | Full nav; session page QR left / roster right; tables show all columns; 32 px page titles |

Horizontal scrolling is allowed **only** inside `.table-responsive`, and only for the failed-emails table and the teacher grade sheet, where every column is needed.

### B7. `DESIGN.md`

Write `DESIGN.md` at the repo root from this spec's sections A–B and the team's brand direction (Impeccable's design file), so later `/impeccable` runs have a direction to check against.

---

## C. Application layout (`ui-foundation`)

`Views/Shared/_Layout.cshtml`, top to bottom:

1. Visually hidden **"Skip to content"** link → `<main id="main">`.
2. **Navbar** on a white surface with a 1 px bottom border (no shadow):
   - Logo: inline teal K + check symbol + "Klasly" wordmark (Inter 600, navy) → `/`, which routes by role (unchanged). Symbol only below `sm`. `aria-label="Klasly home"`.
   - Role nav in navy, collapsing with Bootstrap's `navbar-toggler` + `collapse` (`aria-controls`, `aria-expanded`, label "Menu"). Admin collapses below `lg` (5 items); Teacher and Parent below `sm`.
   - Active item: teal text on pale teal + `aria-current="page"`. **Area-aware**: in the Teacher area, `Classes`, `Sessions` and `Assessments` all highlight "My classes".
   - Right side: user's name (muted, small) + "Log out" (`btn-sm btn-outline-secondary`, unchanged POST form). On phones they sit inside the collapsed menu.
3. `main#main.container` with `<partial name="_Alerts" />` then `@RenderBody()`. A page can set `ViewData["AlertsInPage"] = true` and render `_Alerts` itself (Login, Access denied) so alerts line up with a narrow column.
4. **Footer removed** (it only said "© 2026 Klasly" and took a fixed 60 px band); the absolute-position rules go with it.
5. `<link rel="icon" href="~/favicon.svg" type="image/svg+xml">`; empty import map removed; scripts unchanged in order: jQuery → Bootstrap bundle → `site.js` → `Scripts` section.

**Role navigation**

| Role | Items | Why |
|---|---|---|
| Admin | Terms · Subjects · Accounts · Classes · Failed emails | Setup order; Failed emails last (exception queue) |
| Teacher | My classes | Every teacher task starts from a class; the class page is the hub |
| Parent | My children | One destination; the child switcher on the overview handles several children |

A sidebar was considered for Admin and rejected: 5 items fit one row at `lg`, and a sidebar adds markup and a second responsive pattern for no task gain.

## D. Page patterns and integration

Page-level detail and every "Must preserve" list are in `docs/ui-ux/UI_UX_HANDOFF.md` §5. This spec fixes the patterns those pages follow.

| Pattern | Pages | Structure |
|---|---|---|
| Sign-in | P1 | Canvas background; centered white panel; logo + one line "Attendance and grades for your classes"; form; teal full-width "Log in" with pending state |
| List | P7, P9, P11, P13, P16, P17, P21 | Header (title + one primary action) → filter (if any) → table or empty state |
| Form | P8, P10, P12, P14 | Breadcrumb → title (Edit names the record) → form (fieldsets on P12) → Save/Cancel |
| Detail hub | P15, P18 | Breadcrumb → header + meta + secondary action → main task first → supporting lists |
| Live operation | P19 | Header + status + End session → QR (≥ 320 px, white, quiet zone) + large checked-in count + roster |
| Data entry | P20 | Breadcrumb → header → grade table → sticky Save bar with live "N of M graded" → Edit assessment (`<details>`) |
| Monitoring | P22 | Header → one-line answer ("All classes on track" / "1 of 4 classes needs attention") → one panel per class with labelled attendance numbers |
| Message | P2, P4, P5, Not found | B5 message page |

**Flows that must read as one path** (breadcrumbs + the same names on both ends):
- Admin: Classes → *{code} – {section}* (enroll above roster, dropped grouped) → Edit class.
- Admin: Accounts (*one* "New account" dropdown, filter pills in the same order) → New/Edit {role} → Linked students ("saved immediately" note, separate panel).
- Teacher: My classes (class name is the link) → class page (**live-session strip on top** when one is open; Start form collapsed behind "Start another session" while live) → session page.
- Teacher: class page `#assessments` → grade sheet → breadcrumb back to `#assessments`.
- Parent: My children (whole row is the link) → child overview (switcher pills when > 1 child).

## Tech stack (unchanged)

ASP.NET Core 10 MVC, Razor views, Bootstrap 5.3.3 (local `wwwroot/lib`), jQuery + jQuery Validation Unobtrusive, QRCoder (server-side SVG). New static assets only: Inter woff2 + its license, logo SVGs. **No new packages, no icon library, no CDN, no front-end framework.**

## Commands

```powershell
dotnet build Klasly.slnx
```
```powershell
dotnet test Klasly.slnx
```
Browser check: `preview_start` the `api` then `web` configurations (`.claude/launch.json` → https://localhost:7256, https://localhost:7144), log in with `docs/TEST-ACCOUNTS.md`.
After code changes: `graphify update .`

## Project structure (files this work touches)

```
src/Klasly.Web/
├── wwwroot/css/site.css               ← @font-face, tokens, Bootstrap overrides, shared components (one file)
├── wwwroot/js/site.js                 ← data-confirm (kept) + pending submit + unsaved guard
├── wwwroot/fonts/InterVariable.woff2, OFL.txt   ← ui-brand
├── wwwroot/img/klasly-logo.svg, klasly-mark.svg, klasly-logo-reverse.svg  ← ui-brand
├── wwwroot/favicon.svg                ← ui-brand (template favicon.ico removed)
├── Views/Shared/_Layout.cshtml        ← logo, nav, skip link, alerts slot, no footer
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
/* site.css: font first, then tokens, then Bootstrap overrides, then Klasly components. */
@font-face {
  font-family: "Inter";
  src: url("../fonts/InterVariable.woff2") format("woff2");
  font-weight: 100 900;
  font-display: swap;
}
:root {
  --k-teal: #126b64;
  --k-teal-hover: #0e5650;
  --k-navy: #17324d;
  --bs-primary: var(--k-teal);
  --bs-primary-rgb: 18, 107, 100;
  --bs-link-color-rgb: 18, 107, 100;
  --bs-heading-color: var(--k-navy);
  --bs-body-bg: #f5f7f8;
  --bs-body-color: #263746;
  --bs-body-font-family: "Inter", system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
  --bs-border-color: #dce3e8;
}
.btn-primary {
  --bs-btn-bg: var(--k-teal);
  --bs-btn-border-color: var(--k-teal);
  --bs-btn-hover-bg: var(--k-teal-hover);
  --bs-btn-hover-border-color: var(--k-teal-hover);
}
```

```cshtml
@* Row actions: the name is the link, secondary is outline, destructive is last and confirmed, row context for screen readers. *@
<td><a asp-action="Details" asp-route-id="@c.ClassId" class="fw-semibold">@c.SubjectCode – @c.Section</a></td>
<td class="text-end text-nowrap">
    <a asp-action="Edit" asp-route-id="@c.ClassId" class="btn btn-sm btn-outline-secondary">Edit<span class="visually-hidden"> @c.SubjectCode @c.Section</span></a>
</td>
```

- Presentation only in views: no business rules, no thresholds, no hard-coded 20 % / 75.
- Comment only the *why* a CSS rule exists (one line), as `site.css` already does.

## Testing strategy

There are no automated UI tests for `Klasly.Web`, and this work adds none (Playwright would be a new package, so it's out of scope unless asked). Verification per module:

1. `dotnet build` (0 warnings) and `dotnet test` (all green), to prove nothing server-side broke.
2. **Form-contract check:** before changing a page, record each form's `action`, `method` and the sorted list of input `name`s with `javascript_tool`; after the change, compare. They must match exactly, except where this spec approves a change.
3. **Anti-forgery check:** every `form[method=post]` contains `__RequestVerificationToken`.
4. **Responsive check** at **320, 375, 768 and 1440 px**: `document.documentElement.scrollWidth <= clientWidth`; nav on one row; screenshots for the report.
5. **Contrast check:** computed colors of body, muted, link, button and badge text measured against their real backgrounds (≥ 4.5 : 1 text, ≥ 3 : 1 controls and focus ring).
6. **Flow check** in the browser: each form's success and error path; session page: start, watch the 5 s `Live` polls in network requests, stop the API to see "Reconnecting…", end, correct a row; grade sheet: type, try to leave (warning), save.
7. Keyboard: tab through the skip link, nav, toggler and forms; visible focus everywhere. Console: no errors. Inter actually loads (network 200, `document.fonts.check('16px Inter')`).
8. Logo legible at 16/32/48 px (screenshot).
9. `/impeccable` detect on changed views: no findings.

## Boundaries

- **Always:** keep routes, POST methods, anti-forgery, field names, validation, `[Authorize]`, the live-session contract (`live-panel`, `live-status`, `live-announcer`, single-root `_Live` with `data-closed`/`data-summary`) and QR readability (handoff §8); keep status colors separate from brand teal; verify in a real browser before claiming a visual result; write the CLAUDE.md §5 reports per module.
- **Ask first:** any controller or `Program.cs` change beyond the three approved in Decisions §2; any new API call; any feature from the handoff's "Optional" list; downloading the Inter font file; any Android change; changing user-facing wording about Brevo or push notifications beyond what Open Questions settle.
- **Never:** add packages, frameworks, icon fonts or CDN links; put colors in views; use teal for badges or decoration; fork `_AssessmentForm` or `_Live`; add client-side business rules; work outside the `ui-redesign` branch, or merge/deploy it (klasly.site, `dev`, `master`) before the demo is over; commit unless asked (no Claude attribution).

## Success criteria

- [ ] Logo symbol approved by the team from screenshots; three SVG variants and `favicon.svg` exist; legible at 16 px.
- [ ] Inter is served from `wwwroot/fonts` (no third-party request) with `OFL.txt` alongside; the system stack takes over if the font fails to load.
- [ ] `site.css` defines the B1 tokens; no hex colors in any `.cshtml`; `_Layout.cshtml.css` is gone.
- [ ] Measured contrast meets B1 on white and canvas (muted text ≥ 4.5 : 1 on both).
- [ ] Base font is 16 px at 320 px and at 1440 px; page titles are 28 px on phones and 32 px on desktop.
- [ ] At 320/375/768/1440 px, no page scrolls sideways except inside the two allowed tables; the navbar is one row with a working toggler where B6 says it collapses.
- [ ] "My classes" is highlighted on every `/Teacher/*` page.
- [ ] Every status in B2 renders through `_StatusBadge` with the listed variant; no teal badges.
- [ ] Form-contract and anti-forgery checks pass on every page with a form (P1, P7–P15, P18–P20).
- [ ] A valid submit disables its button with pending text; a cancelled confirm or a failed client validation does not.
- [ ] Leaving the grade sheet with typed, unsaved scores warns; saving does not.
- [ ] The teacher class page with an open session shows the live-session strip with "Show QR" above everything else.
- [ ] The two forms on the teacher class page never show the same error twice.
- [ ] The Error page has no "Development Mode" text; a bad id (e.g. `/Admin/Classes/Details/999999`) shows a styled Not found page with navigation, still with HTTP 404.
- [ ] After a correction, the session page opens scrolled to the corrected student's row.
- [ ] The grade sheet no longer mentions phone notifications; the failed-emails page text no longer says "Brevo".
- [ ] Session page at 375 px and 1440 px: QR ≥ 320 px wide on white; `Live` polled every ~5 s; the reconnect warning appears when the API stops.
- [ ] The parent overview shows a one-line answer above the class panels, and at-risk classes come first.
- [ ] No footer on any page.
- [ ] `dotnet build` 0 warnings, `dotnet test` all green, `/impeccable` detect clean, reports written for each module.

## Decisions (answered 2026-11-20)

1. **Branch:** all work happens on a new branch `ui-redesign` off `dev`. Nothing is merged or deployed until after the demo on 2026-11-21.
2. **Controller / Program changes: all three approved.**
   - P1-2 styled Not found page (`Program.cs` `UseStatusCodePagesWithReExecute` + `HomeController.StatusCode` + `NotFound.cshtml`).
   - P1-5 form-specific error keys in `SessionsController.Start` / `AssessmentsController.Create`, so each form on the teacher class page shows only its own error.
   - P3-3 return to the corrected row (`RedirectToAction(..., fragment: "row-{studentId}")` in `SessionsController.Correct`, plus `id="row-{studentId}"` on roster rows).
3. **Impeccable file:** none exists outside the repo. Write `DESIGN.md` (B7).
4. **Parent overview:** at-risk classes are listed first (view-only ordering; the API order is kept within each group).
5. **Grade sheet:** remove the sentence "A student's phone is notified when their first grade is saved." (Firebase push is deferred, commit `9615d2b`.)
6. **Footer:** removed entirely.
7. **Failed emails page:** user-facing text says "the email service" instead of "Brevo" (docs and code names keep "Brevo").
8. **Contrast (approved 2026-11-20):** muted text `#5A6A80` instead of `#64748B` (which is 4.4 : 1 on the canvas), and `#8696A7` for form-control outlines only (3.0 : 1).
9. **Inter (approved 2026-11-20):** bundle `InterVariable.woff2` (v4.1) and `OFL.txt` from the official rsms/inter GitHub release, about 340 KB. The system stack stays as the fallback.
10. **Android icon (approved 2026-11-20):** add adaptive-icon vector drawables (monogram foreground, teal background). This PC has no JDK, so a teammate runs `./gradlew assembleDebug` after the change.

## Open questions

None. The three questions above were answered as Decisions 8–10.

## Out of scope

API changes; Android screens beyond the app icon (Q5); new features from the handoff's Optional list (full-screen QR, account search, searchable pickers, active-term status on Admin home, list-page live/at-risk indicators, dark mode) unless asked.
