# Klasly Web MVC: UI/UX Audit and Handoff Specification

> **Audience:** the AI coding agent that will improve the look and usability of the Klasly website (`src/Klasly.Web`).
> **Status:** analysis only. No application code, view, CSS, JavaScript, controller, model or configuration was changed while writing this.
> **Audit date:** 2026-11-20. **Branch audited:** `dev` (latest commit `9615d2b`).
> **Method:** every controller, view, partial, view model, API DTO, service, CSS and JS file in `src/Klasly.Web` was read. The rendered pages were **not** opened in a browser during this audit. Where a statement depends on how the page renders, it is marked **Needs verification**. Earlier rendered measurements come from the team's own reports in `docs/reports/`.

---

## Contents

1. [Project overview](#1-project-overview)
2. [Application architecture relevant to the UI](#2-application-architecture-relevant-to-the-ui)
3. [Existing design direction (Impeccable)](#3-existing-design-direction-impeccable)
4. [Complete webpage inventory](#4-complete-webpage-inventory)
5. [Page-by-page documentation](#5-page-by-page-documentation)
6. [Application-wide design analysis](#6-application-wide-design-analysis)
7. [Shared component inventory](#7-shared-component-inventory)
8. [Functional preservation requirements (global)](#8-functional-preservation-requirements-global)
9. [Prioritized UI/UX roadmap](#9-prioritized-uiux-roadmap)
10. [Instructions for the next AI agent](#10-instructions-for-the-next-ai-agent)
11. [Audit coverage check and limitations](#11-audit-coverage-check-and-limitations)

How to read each page section: **"Current"** subsections describe what exists in the code today. **"Recommended"** subsections are proposals. Nothing under "Recommended" exists yet.

---

## 1. Project overview

**Klasly: Smart Campus Attendance and Academic Monitoring.** It's a student project (ELECT3 / MobDev) with four roles:

| Role | Where they work | What they do |
|---|---|---|
| Admin | Website, `/Admin` area | Sets up terms, the subject list, accounts (and parent–child links), classes and enrollments. Reviews failed parent emails. |
| Teacher | Website, `/Teacher` area | Starts class sessions, shows a rotating QR code that students scan, watches attendance arrive live, corrects attendance after a session, creates assessments and enters grades. |
| Parent | Website, `/Parent` area (Android too) | Sees each linked child's attendance, grades and at-risk status for the active term. |
| Student | **Android app only** | Scans the QR code and sees their own records. The website refuses student logins (`Services/AuthService.cs:18`). |

The website is a **task tool used in short, focused visits**: a teacher starting a session at the front of a room, an admin entering records, a parent checking on a child from a phone. Its job is speed, clarity and trust, not marketing.

Tech: ASP.NET Core 10 MVC, Razor views, **Bootstrap 5.3.3** (from `wwwroot/lib`), jQuery + jQuery Validation Unobtrusive (client-side validation only), QRCoder 1.8.1 (server-side QR SVG). No front-end framework, no build step for CSS/JS, no icon library, no web fonts.

---

## 2. Application architecture relevant to the UI

### 2.1 Request flow

```
Browser ──> MVC Controller (Areas/*/Controllers) ──> Web Service (Services/) ──> API-backed Repository (Repositories/)
        <── Razor View (+ partials)               <── ViewModel              <── typed HttpClient + JWT ──> Klasly.API
```

- **The website has no database.** All data comes from the Klasly Web API through typed `HttpClient` repositories (`Program.cs:47-65`). Business rules (weights total 100%, max score, who owns which class, at-risk calculation) live in the API, **not** in the views or Web services.
- **Login:** `POST /Account/Login` calls `POST api/auth/login`. The returned JWT is stored inside the encrypted, HttpOnly `Klasly.Auth` cookie (`Program.cs:17-41`, `Services/AuthService.cs`). `Infrastructure/BearerTokenHandler.cs` attaches it to every API call. The cookie is non-persistent and expires exactly with the JWT.
- **One error funnel:** `Infrastructure/ApiExceptionFilter.cs` (global filter) turns API failures into pages:
  - API **401** → sign out, flash "Your session ended, please log in again.", redirect to Login (keeps the return URL for GET pages only).
  - API **403** → flash the API's reason, redirect to `/Account/AccessDenied`.
  - API **404** → `NotFoundResult` (rendered by `UseStatusCodePages()`; see §6.4).
  - API unreachable / timeout (15 s) → `Views/Shared/ApiUnavailable.cshtml` with HTTP 503.
  - API **400 / 409** are *not* handled here. Each controller catches them and shows them on the form (`ModelStateExtensions.AddApiErrors`) or as a flash message (`TempData["Error"]`).
- **Flash messages:** controllers set `TempData["Success"]` / `TempData["Error"]` and redirect. `Views/Shared/_Alerts.cshtml` (rendered by the layout above every page body) shows them.
- **Times:** the API sends UTC. `Infrastructure/ManilaTime.cs` converts for display (`Format` → "Nov 10, 9:00 AM", `FormatTime` → "9:00 AM") and converts typed `datetime-local` values to `+08:00` for the API.

### 2.2 Routing

`Program.cs:94-102`:
- `areas` route: `{area:exists}/{controller=Home}/{action=Index}/{id?}`
- `default` route: `{controller=Home}/{action=Index}/{id?}`

Areas: `Admin`, `Teacher`, `Parent`. Each area's controllers carry `[Area("…"), Authorize(Roles = "…")]` at class level.

### 2.3 Layout and view plumbing

- Every view (root and areas) uses `Views/Shared/_Layout.cshtml` (via each `_ViewStart.cshtml`).
- Area `_ViewImports.cshtml` files add `Klasly.Web.Models.Api` (DTOs) and, for Teacher/Parent, `Klasly.Web.Infrastructure` (for `ManilaTime`).
- Scripts: the layout loads jQuery, `bootstrap.bundle.min.js` and `js/site.js` on every page, then `@RenderSection("Scripts")`. Form pages add `_ValidationScriptsPartial` in that section.

### 2.4 Where to log in for testing

Test accounts are listed in `docs/TEST-ACCOUNTS.md` (untracked file, local only). Local run: `.claude/launch.json` has `api` (https://localhost:7256) and `web` (https://localhost:7144). The Web's API base URL is `Api:BaseUrl` in `src/Klasly.Web/appsettings.Development.json`. Start the API first, or every page shows the "can't reach the server" page.

---

## 3. Existing design direction (Impeccable)

**Important finding: there is no Impeccable `PRODUCT.md` or `DESIGN.md` anywhere in the repository.** Several reports say so explicitly (e.g. `docs/reports/2026-11-11-1045-milestone-13-task-5-parent-portal-polish/DEVELOPER-DOCS.md:51`: "No PRODUCT.md / DESIGN.md; `/impeccable init` could capture them later"). **Needs verification:** if the team set a design direction in Impeccable outside the repo, the next agent must get those files from the team before redesigning.

What *is* documented is the direction applied in earlier `/impeccable` passes. Treat these as the current design rules:

| Decision | Source |
|---|---|
| **Operate mode**: task screens; the bar is precision and consistency, "with no new visual style". A refinement of the incumbent Bootstrap look. | `docs/reports/2026-11-09-2101-milestone-5-web-admin/DEVELOPER-DOCS.md:59`, `docs/reports/2026-11-10-1811-milestone-9-worker-absent-corrections/DEVELOPER-DOCS.md:46` |
| **The Teacher area is the authority** for house style when no design file exists. | `docs/reports/2026-11-11-1045-…-parent-portal-polish/DEVELOPER-DOCS.md:4` |
| Admin home is a **list**, not a card grid. | M5 report line 59 |
| Row-level destructive actions are **quiet `btn-link link-danger`**, not red outlined buttons. | M5 report line 59 |
| **Tabular numerals** in tables (`site.css:34`). Student numbers and times are `text-nowrap`. | M5, M7 reports |
| Visually hidden `h2`s give screen readers a heading structure on the session page. | M7 report line 85 |
| The QR code always has a fixed white background with quiet zone, min 320 px wide. | `site.css:53-68`, M7 report |
| Phone layout: hide secondary columns (`d-none d-sm-table-cell` / `d-md-table-cell`) so the key action stays on screen; long text cells use `.cell-readable` and the table scrolls inside `.table-responsive`. | M9, M10, failed-emails reports |
| Parent overview: expandable sections get a ~42 px tap target (`.overview-toggle`); Absent is emphasized; "Not graded yet" is muted. | M13 Task 5 report |
| No dark mode exists; this was noted and not requested. | M7 report line 79 |

Every earlier pass ran `impeccable detect` and reported no findings.

**Consequence for the next agent:** "polished and professional" must be reached by **tightening the existing Bootstrap system** (tokens, spacing, hierarchy, consistent components), not by introducing a new visual language. Avoid gradients, decorative illustrations, glassmorphism, stat-tile dashboards with no decision behind them, oversized rounded cards and icon-heavy chrome.

---

## 4. Complete webpage inventory

Legend: **Auth** = the authorization that applies. "Anonymous" = no `[Authorize]`.

### 4.1 Pages that render a full view

| # | Page | Route (GET) | Controller.Action | View | Auth |
|---|---|---|---|---|---|
| P1 | Log in | `/Account/Login?returnUrl=` | `AccountController.Login` | `Views/Account/Login.cshtml` | Anonymous (signed-in users are redirected to `/`) |
| P2 | Access denied | `/Account/AccessDenied` | `AccountController.AccessDenied` | `Views/Account/AccessDenied.cshtml` | Anonymous |
| P3 | Home (role router / student fallback) | `/` , `/Home/Index` | `HomeController.Index` | `Views/Home/Index.cshtml` (fallback only) | `[Authorize]` |
| P4 | Error (production only) | `/Home/Error` | `HomeController.Error` | `Views/Shared/Error.cshtml` | Anonymous |
| P5 | Server unavailable | *(any page when the API is down)* | `ApiExceptionFilter` | `Views/Shared/ApiUnavailable.cshtml` | Whatever the original page required |
| P6 | Admin home | `/Admin` | `Admin/HomeController.Index` | `Areas/Admin/Views/Home/Index.cshtml` | Admin |
| P7 | Terms list | `/Admin/Terms` | `Admin/TermsController.Index` | `Areas/Admin/Views/Terms/Index.cshtml` | Admin |
| P8 | New / Edit term | `/Admin/Terms/Create`, `/Admin/Terms/Edit/{id}` | `TermsController.Create`, `.Edit` | `Areas/Admin/Views/Terms/Form.cshtml` | Admin |
| P9 | Subjects list | `/Admin/Subjects` | `Admin/SubjectsController.Index` | `Areas/Admin/Views/Subjects/Index.cshtml` | Admin |
| P10 | New / Edit subject | `/Admin/Subjects/Create`, `/Admin/Subjects/Edit/{id}` | `SubjectsController.Create`, `.Edit` | `Areas/Admin/Views/Subjects/Form.cshtml` | Admin |
| P11 | Accounts list | `/Admin/Accounts?role=` | `Admin/AccountsController.Index` | `Areas/Admin/Views/Accounts/Index.cshtml` | Admin |
| P12 | New / Edit account (+ parent links) | `/Admin/Accounts/Create?role=`, `/Admin/Accounts/Edit/{id}` | `AccountsController.Create`, `.Edit` | `Areas/Admin/Views/Accounts/Form.cshtml` + `_ParentLinks.cshtml` | Admin |
| P13 | Classes list | `/Admin/Classes?termId=` | `Admin/ClassesController.Index` | `Areas/Admin/Views/Classes/Index.cshtml` | Admin |
| P14 | New / Edit class | `/Admin/Classes/Create`, `/Admin/Classes/Edit/{id}` | `ClassesController.Create`, `.Edit` | `Areas/Admin/Views/Classes/Form.cshtml` | Admin |
| P15 | Class details (enrollments) | `/Admin/Classes/Details/{id}` | `ClassesController.Details` | `Areas/Admin/Views/Classes/Details.cshtml` | Admin |
| P16 | Failed emails | `/Admin/FailedEmails` | `Admin/FailedEmailsController.Index` | `Areas/Admin/Views/FailedEmails/Index.cshtml` | Admin |
| P17 | My classes (teacher) | `/Teacher/Classes` | `Teacher/ClassesController.Index` | `Areas/Teacher/Views/Classes/Index.cshtml` | Teacher |
| P18 | Teacher class page | `/Teacher/Classes/Details/{id}` | `Teacher/ClassesController.Details` | `Areas/Teacher/Views/Classes/Details.cshtml` + `_StartSession`, `_AssessmentForm`, `_SessionStatusBadge` | Teacher |
| P19 | Session page (QR + live roster / corrections) | `/Teacher/Sessions/Details/{id}` | `Teacher/SessionsController.Details` | `Areas/Teacher/Views/Sessions/Details.cshtml` + `_Live.cshtml`, `_SessionStatusBadge` | Teacher |
| P20 | Assessment grade sheet | `/Teacher/Assessments/Details/{id}` | `Teacher/AssessmentsController.Details` | `Areas/Teacher/Views/Assessments/Details.cshtml` + `_AssessmentForm` | Teacher |
| P21 | My children (parent) | `/Parent/Children` | `Parent/ChildrenController.Index` | `Areas/Parent/Views/Children/Index.cshtml` | Parent |
| P22 | Child overview | `/Parent/Children/Details/{id}` | `Parent/ChildrenController.Details` | `Areas/Parent/Views/Children/Details.cshtml` | Parent |

Plus the built-in plain-text status page from `UseStatusCodePages()` for 404 and other empty error responses (see §6.4). It has no view.

### 4.2 Actions without their own view

| Route | Method | Action | Result |
|---|---|---|---|
| `/Account/Login` | POST | `AccountController.Login` | Sign in → `returnUrl` (local only) or `/`; errors re-render P1 |
| `/Account/Logout` | POST | `AccountController.Logout` | Sign out → `/Account/Login` |
| `/Admin/Terms/Create`, `/Admin/Terms/Edit/{id}` | POST | `TermsController.Create/Edit` | → P7 with success flash; errors re-render P8 |
| `/Admin/Terms/Activate/{id}` | POST | `TermsController.Activate` | → P7, flash "{name} is now the active term." |
| `/Admin/Terms/Deactivate/{id}` | POST | `TermsController.Deactivate` | → P7, flash |
| `/Admin/Subjects/Create`, `/Admin/Subjects/Edit/{id}` | POST | `SubjectsController.Create/Edit` | → P9; errors re-render P10 |
| `/Admin/Accounts/Create` | POST | `AccountsController.Create` | → P11 filtered to the role; errors re-render P12 |
| `/Admin/Accounts/Edit/{id}` | POST | `AccountsController.Edit` | → P11 filtered to the role; errors re-render P12 (reloads parent links) |
| `/Admin/Accounts/Deactivate/{id}?role=` | POST | `AccountsController.Deactivate` | → P11 (same filter), success or error flash |
| `/Admin/Accounts/ResetDevice/{id}?role=` | POST | `AccountsController.ResetDevice` | → P11 (same filter) |
| `/Admin/Accounts/LinkStudent/{id}` | POST | `AccountsController.LinkStudent` (`studentId`, `relationship`, `receivesAlerts`) | → P12 Edit, flash |
| `/Admin/Accounts/UnlinkStudent/{id}?studentId=` | POST | `AccountsController.UnlinkStudent` | → P12 Edit, flash |
| `/Admin/Classes/Create`, `/Admin/Classes/Edit/{id}` | POST | `ClassesController.Create/Edit` | → P13 for that term; errors re-render P14 (rebuilds dropdowns) |
| `/Admin/Classes/Enroll/{id}` | POST | `ClassesController.Enroll` (`studentId`) | → P15, flash |
| `/Admin/Classes/Drop/{id}?enrollmentId=` | POST | `ClassesController.Drop` | → P15, flash |
| `/Teacher/Sessions/Start` | POST | `SessionsController.Start` (`ClassId`, `EndTime`, `LateAfterMinutes`) | Creates **and opens** a session → P19; errors re-render **P18** via explicit path |
| `/Teacher/Sessions/Live/{id}` | GET | `SessionsController.Live` | Partial `_Live.cshtml`, polled every 5 s by P19, `no-store` |
| `/Teacher/Sessions/Open/{id}` | POST | `SessionsController.Open` | → P19, flash |
| `/Teacher/Sessions/Close/{id}` | POST | `SessionsController.Close` | → P19, flash "Session ended." |
| `/Teacher/Sessions/Correct/{id}?studentId=` | POST | `SessionsController.Correct` (`status`, `currentStatus`) | → P19, flash |
| `/Teacher/Assessments/Create` | POST | `AssessmentsController.Create` | → P20 of the new assessment; errors re-render **P18** via explicit path |
| `/Teacher/Assessments/Edit/{id}` | POST | `AssessmentsController.Edit` | → P20; errors re-render P20 with the edit section open |
| `/Teacher/Assessments/SaveGrades/{id}` | POST | `AssessmentsController.SaveGrades` (`Grades[i].StudentId`, `Grades[i].Score`) | → P20; errors re-render P20 keeping typed values |
| `/Parent/Children` | GET | `ChildrenController.Index` | **Redirects to P22 when the parent has exactly one child** |
| `/` | GET | `HomeController.Index` | Admin → `/Admin`; Teacher → `/Teacher/Classes`; Parent → `/Parent/Children` |

### 4.3 Navigation map

```
Login ─┬─ Admin ──> /Admin (home list) ─┬─ Terms ── New/Edit term
       │                                ├─ Subjects ── New/Edit subject
       │                                ├─ Accounts ── New <role> / Edit (parent: Linked students)
       │                                ├─ Classes ── New/Edit class
       │                                │          └─ Students (Details: enroll / drop)
       │                                └─ Failed emails   (navbar only; not on the home list)
       ├─ Teacher ─> My classes ── Class page ─┬─ Start session ──> Session page (QR, live roster)
       │                                       ├─ Session row "Show QR"/"View" ──> Session page (corrections when closed)
       │                                       └─ Assessment "Grades" / Add ──> Grade sheet (+ Edit assessment)
       └─ Parent ──> My children ──> Child overview (switcher pills between children)
                     (skipped when only one child)
```

Navbar items per role (`_Layout.cshtml:18-25`): Admin → Terms, Subjects, Accounts, Classes, Failed emails. Teacher → My classes. Parent → My children. The "Klasly" brand always links to `/`, which routes by role.

---

## 5. Page-by-page documentation

### P0. Shared layout: `Views/Shared/_Layout.cshtml` (applies to every page)

**Identity:** master layout. No controller. Used by all 22 pages.

**Current structure (top to bottom)**
1. `<header>` with a Bootstrap `navbar navbar-expand-sm navbar-light bg-white border-bottom box-shadow mb-3`, inside `.container`:
   - Brand "Klasly" (text only, `fw-semibold`) → `/`.
   - Role nav (`ul.navbar-nav.flex-row.gap-3.me-auto`). Active item gets `active fw-semibold` and `aria-current="page"` when the **current controller name** matches the item's controller.
   - When signed in: the user's full name (`text-body-secondary small`) and a "Log out" `btn-sm btn-outline-secondary` inside a POST form.
   - **No `navbar-toggler` / collapse.** The items use `flex-row`, so on narrow screens they wrap onto extra lines (the M5 report confirms "nav wraps" at 375 px).
2. `.container > main.pb-3` → `<partial name="_Alerts" />`, then `@RenderBody()`.
3. `<footer class="border-top footer text-muted">` "© 2026 Klasly". It is absolutely positioned at the bottom (`_Layout.cshtml.css:42-48`, `site.css:15-22`, the default template sticky footer).
4. Scripts: jQuery, Bootstrap bundle, `site.js`, then the `Scripts` section.

**Current functionality**
- Role-aware navigation and logout (POST with anti-forgery token, added automatically by the form tag helper).
- `<title>` = `{ViewData["Title"]} - Klasly`.
- Empty `<script type="importmap"></script>` (template leftover, unused).

**UI/UX assessment**
- Navigation does not collapse on phones; the Admin bar (5 items + name + Log out) wraps into several rows and pushes content down.
- Active state is wrong on the deeper Teacher pages: on `/Teacher/Sessions/*` and `/Teacher/Assessments/*` the current controller is `Sessions`/`Assessments`, so **no nav item is highlighted**, although the user is inside "My classes".
- The footer carries no useful information and takes a fixed 60 px band.
- Flash alerts render full container width above the page's own breadcrumb and heading, even on narrow centered pages (Login), so they do not line up with the content column.
- There is no "skip to main content" link (minor with a short nav).
- `_Layout.cshtml.css` is the unmodified template file. Because it is CSS-isolated, its `.btn-primary`, `.nav-pills` and `button.accept-policy` rules only match elements in the layout itself, which has none of those. They are dead code. Its `a { color: #0077cc }` only reaches layout links, so the brand link probably renders `#0077cc` while page links use Bootstrap's `#0d6efd`. **Needs verification** in the browser.

**Recommended improvements**
- Add a standard Bootstrap collapse (`navbar-toggler` + `collapse navbar-collapse`; Bootstrap JS is already loaded) for widths below `sm`/`md`, so the bar stays one row on phones. Keep the user's name and Log out inside the collapsed menu on phones.
- Make the active-item logic area-aware: in the Teacher area, treat `Classes`, `Sessions` and `Assessments` as "My classes". Keep `aria-current="page"`.
- Give the brand a small, real identity: a wordmark with one brand color. No illustrations or gradients. Replace the template favicon (**Needs verification** that `wwwroot/favicon.ico` is the template icon).
- Remove the absolute-positioned footer (and the `body { margin-bottom: 60px }` / `html { position: relative }` support rules) or replace it with a normal-flow footer. Ask the team whether any footer text is required.
- Move the alerts into the content flow so that each page decides where they sit (or at least constrain them to the page's content width). Make success alerts dismissible (`alert-dismissible` + close button). Keep `role="status"` for success and `role="alert"` for errors.
- Delete the dead rules in `_Layout.cshtml.css` and the empty import map, and move the real color decisions into tokens (see §6.1).
- Add a visually hidden "Skip to content" link targeting `<main id="main">`.

**Must preserve**
- The logout form: `asp-area="" asp-controller="Account" asp-action="Logout" method="post"` (tag helper → anti-forgery token).
- Role detection via `User.IsInRole(...)` and the nav item → controller mapping (`Terms`, `Subjects`, `Accounts`, `Classes`, `FailedEmails`; `Classes`; `Children`).
- `<partial name="_Alerts" />` must still render on every page (all flash messages depend on it).
- `@RenderSection("Scripts", required: false)` after jQuery and `site.js` (validation and the session poller depend on the order).
- `lang="en"`, viewport meta, `asp-append-version` on site assets.

---

### P1. Log in

**Identity**
- Route: `GET/POST /Account/Login?returnUrl=…`
- Controller/Action: `Controllers/AccountController.cs` → `Login` (GET line 13, POST line 21)
- View: `Views/Account/Login.cshtml`; model `ViewModels/LoginViewModel.cs`
- Auth: anonymous. A signed-in user is redirected to `/`.
- Purpose: the sign-in page for admins, teachers and parents.

**Current structure**
1. Layout navbar (brand only, no nav items).
2. Centered column (`row justify-content-center py-5`, `col-12 col-sm-9 col-md-6 col-lg-4`).
3. `h1.h3` "Log in to Klasly"; muted subtitle "For admins, teachers and parents."
4. Form (`novalidate`): validation summary (model-level, `alert alert-danger`), hidden `ReturnUrl`, Email (`autocomplete="username"`, `autofocus`), Password (`autocomplete="current-password"`), full-width primary "Log in" button.
5. Footer.

**Current functionality**
- Client validation (unobtrusive): "Enter your email." / "Enter a valid email address." / "Enter your password."
- Server errors in the summary: "Invalid email or password." (API returned no login); "Students use the Klasly mobile app." (student account); API 400/409 message.
- After an expired login, the layout's alert shows "Your session ended, please log in again." (set in `Program.cs:34` and `ApiExceptionFilter.cs:24`).
- On success, redirects to `ReturnUrl` only when it is a local URL, else `/`.
- API down → P5.
- No loading state on submit.

**UI/UX assessment**
- The page is functional and accessible (labels, autocomplete, autofocus, error summary with `role="alert"`).
- It has no identity beyond the word "Klasly"; it's the first impression for every user and looks like the default template.
- The "session ended" alert spans the full container width above the narrow centered column, so it looks detached from the form.
- No pending state: a slow API (up to 15 s timeout) gives no feedback after pressing "Log in"; users may press again.
- No show/hide password control (optional).

**Recommended improvements**
- Keep the single centered column. Add a restrained brand moment above the form: the wordmark plus one line of plain product context, e.g. "Attendance and grades for your classes". No hero image, no gradient, no feature list.
- Render the session-ended / error alert inside the form column, aligned with the inputs.
- On submit, disable the button and change its label to "Logging in…" (keep the form posting normally; see the shared double-submit behavior in §7).
- Optional: a "Show password" toggle button with `aria-pressed`.
- Keep the visible labels (don't switch to placeholder-only or floating labels without labels).

**Must preserve**
- `asp-action="Login" method="post"`, field names `Email`, `Password`, hidden `ReturnUrl` (open-redirect protection depends on `Url.IsLocalUrl` in the controller, not the view).
- `asp-validation-summary="ModelOnly"` (server errors are model-level) and the two `asp-validation-for` spans.
- `autocomplete` attributes and `_ValidationScriptsPartial` in the Scripts section.

---

### P2. Access denied

**Identity**
- Route: `GET /Account/AccessDenied`; `AccountController.AccessDenied`; view `Views/Account/AccessDenied.cshtml`; anonymous.
- Reached in two ways: (a) the cookie handler's `AccessDeniedPath` when a signed-in user opens another role's area (e.g. a teacher opening `/Admin`); (b) `ApiExceptionFilter` on an API 403, which also flashes the API's reason (e.g. "You can only view your own children").

**Current structure**
`section.py-5`: `h1.h3` "You don't have access to that page."; muted text "It belongs to another account. If you think this is wrong, ask the school admin."; `btn-outline-primary` "Go to my home page" → `/`.

**Current functionality**
- Shows the API's 403 reason in the layout alert above the heading when there is one.
- The button routes the user back to their own portal via `HomeController.Index`.

**UI/UX assessment**
- The copy "It belongs to another account." is wrong for case (a): a teacher opening an Admin page is refused because of their role, not because the page belongs to another account.
- When the API reason is present, the page shows two messages that say almost the same thing (alert + heading).

**Recommended improvements**
- Make the body copy correct for both cases, e.g. "Your account can't open this page. If you think this is wrong, ask the school admin." Keep the heading short.
- Show the API reason (when present) as the page's secondary text instead of a separate red alert. That needs the view to read `TempData["Error"]` itself. Note that `_Alerts` also reads and consumes it, so coordinate (for example, render the reason inside the page and skip `_Alerts` here).
- Keep a single clear primary action back home.

**Must preserve**
- Route and anonymous access (`Program.cs:26` points the cookie handler here).
- The "home" link must go through `/` (`asp-controller="Home" asp-action="Index"`), which routes by role.

---

### P3. Home (role router and student fallback)

**Identity**
- Route: `GET /` and `/Home/Index`; `Controllers/HomeController.cs:12`; `[Authorize]`; view `Views/Home/Index.cshtml`.
- Purpose: redirect each role to its portal. The view itself is a fallback for any other signed-in role.

**Current structure / functionality**
- Admin → `/Admin`, Teacher → `/Teacher/Classes`, Parent → `/Parent/Children`. Not signed in → Login.
- The view says "Hi, {name}." and "Use the Klasly app on your phone to scan and see your records."
- **Effectively unreachable today:** students are refused at login (`AuthService.cs:18`), and no other role exists. It only renders if a student cookie somehow exists.

**UI/UX assessment** — nothing material; it's a safety net.

**Recommended improvements** — none required. If touched, keep it a plain message.

**Must preserve** — the redirect logic in `HomeController.Index` (the brand link, Access denied page and login success all rely on it).

---

### P4. Error page (production only)

**Identity**
- Route: `/Home/Error`; `HomeController.Error`; view `Views/Shared/Error.cshtml`; model `Models/ErrorViewModel.cs`.
- Only used outside Development (`Program.cs:79-83`, `UseExceptionHandler("/Home/Error")`). In Development the developer exception page shows instead.

**Current structure**
The unmodified ASP.NET template: red "Error." heading, red `h2` "An error occurred while processing your request.", the Request ID, then a **"Development Mode" section telling the reader to set `ASPNETCORE_ENVIRONMENT`**.

**UI/UX assessment**
- **Critical:** the live site (klasly.site, per commit `00336a5`) shows developer instructions to teachers and parents whenever an unexpected exception happens. It's confusing and unprofessional, and it advertises configuration details.
- Visual tone (red headings, three heading levels) is alarming for a non-technical user.

**Recommended improvements**
- Replace the content with a calm, user-facing message in the same style as `ApiUnavailable.cshtml`: one `h1.h3` ("Something went wrong on our side."), one line of guidance ("Try again. If it keeps happening, tell the school admin."), a "Go to my home page" button, and the Request ID in small muted text ("Reference: …") so support can find the log.
- Remove the "Development Mode" section entirely.

**Must preserve**
- `@model ErrorViewModel`, `Model.ShowRequestId` / `Model.RequestId`, the `[ResponseCache(NoStore)]` on the action.

---

### P5. Server unavailable

**Identity**
- No route; rendered by `ApiExceptionFilter.cs:41-44` with HTTP 503 whenever the API cannot be reached or times out (15 s). View: `Views/Shared/ApiUnavailable.cshtml`.

**Current structure**
`section.py-5`: `h1.h3` "Klasly can't reach the server right now."; muted "Try again in a minute. If it keeps happening, check that the Klasly Web API is running."; an `<a href="" onclick="location.reload()">` styled as `btn-outline-primary` "Try again".

**UI/UX assessment**
- "Check that the Klasly Web API is running" is developer wording; parents and teachers can't act on it.
- The "Try again" control is a link acting as a button (`href=""` + inline `onclick`). It works, but a `<button type="button">` is the correct element. Reloading after a failed **POST** re-submits the form (the browser asks to resend). That's acceptable, but worth knowing.
- The layout still shows the navbar, which is good (the user can navigate away).

**Recommended improvements**
- User-facing copy: "Try again in a minute. If it keeps happening, tell the school admin."
- Use a real `<button type="button">` for "Try again".
- Match the visual pattern of P2 and P4 (one shared "message page" pattern; see §7).

**Must preserve** — the view name `ApiUnavailable` (hard-coded in the filter) and that it renders inside the normal layout.

---

### P6. Admin home

**Identity**
- Route: `GET /Admin`; `Areas/Admin/Controllers/HomeController.cs`; view `Areas/Admin/Views/Home/Index.cshtml`; Admin only.
- Purpose: the admin's landing page and short guide to the four setup sections.

**Current structure**
`h1.h3` "Admin"; muted "Set up the term before teachers open sessions."; a `list-group` (`col-12 col-lg-8`) of four links (Terms, Subjects, Accounts, Classes), each with a bold title and a one-line muted description. The list is hard-coded in the view.

**Current functionality** — navigation only. No data is loaded.

**UI/UX assessment**
- **Failed emails is missing from the list**, although it's in the navbar. An admin who never looks at the navbar won't find it.
- The page gives no sign of the system's state. The instruction "Set up the term before teachers open sessions" can't tell the admin whether a term is active.
- The list-not-cards decision was deliberate (M5 Impeccable pass). Keep it.

**Recommended improvements**
- Add "Failed emails" to the list with a description such as "Parent alerts that couldn't be delivered."
- Optional (needs a team decision, since it adds API calls to a page that has none): a short status line at the top, for example "Active term: 1st Sem 2026-27" or a warning "No active term. Teachers can't run sessions until one is active." with a link to Terms. Don't build a dashboard of counters.
- Order the list in setup order (it already is: Terms → Subjects → Accounts → Classes).

**Must preserve** — the four link targets (`asp-controller` = `Terms`, `Subjects`, `Accounts`, `Classes`, action `Index`, area inherited).

---

### P7. Terms list

**Identity**
- Route: `GET /Admin/Terms`; `TermsController.Index`; view `Areas/Admin/Views/Terms/Index.cshtml`; model `List<TermResponse>`; Admin.
- Purpose: list academic terms and choose which one is active (only one can be active; enforced by the API).

**Current structure**
1. Header row: `h1.h3` "Terms" + primary "New term" (right).
2. Empty state: muted "No terms yet. Create the first semester to get started."
3. Table (`table align-middle` in `.table-responsive`): Name · Start · End · Status · (visually hidden "Actions").
   - Dates: `MMM d, yyyy`.
   - Status: green "Active" badge only for the active term; **empty cell** for others.
   - Actions (`text-end text-nowrap`): "Edit" (`btn-sm btn-outline-secondary`) + either "Deactivate" (`btn-sm btn-link link-danger`, confirm "Deactivate {name}? No term will be active.") or "Make active" (`btn-sm btn-outline-primary`, no confirm).

**Current functionality**
- Make active → POST `Activate/{id}` → flash "{name} is now the active term." (the API deactivates the previous one).
- Deactivate → confirm → POST `Deactivate/{id}` → flash "Term deactivated. No term is active until you make one active."

**UI/UX assessment**
- Three different button styles in one action cell (outline grey, outline blue, link red) give uneven weight and height.
- "Make active" changes which term the whole school runs on but has no confirmation. Deactivate has one.
- Inactive terms show nothing in Status, so the column looks half empty.
- There is no visible "current term" emphasis apart from the badge.

**Recommended improvements**
- Status column: show "Active" (success badge) and "Inactive" (quiet muted text, not a badge) so every row has a value.
- Row actions: follow the shared row-action convention (§7): "Edit" as a quiet link or small outline button, the state change as a second control of the same size, destructive in `link-danger`. Keep them in a stable order: Edit, then the state change.
- Add a confirm to "Make active" that names the term being replaced, e.g. "Make {name} the active term? {current} will be deactivated." The current active term's name is on the page (it's in `Model`).
- Optional: sort or visually emphasize the active term row (e.g. `fw-semibold` name).
- Give each row action an accessible name with context (e.g. visually hidden " {term name}").

**Must preserve**
- POST forms to `Activate` / `Deactivate` with `asp-route-id="@term.TermId"` (tag helper adds anti-forgery).
- `data-confirm` attribute mechanism (`site.js`).
- Links to `Create` and `Edit/{id}`.

---

### P8. New / Edit term (form)

**Identity**
- Routes: `GET/POST /Admin/Terms/Create`, `GET/POST /Admin/Terms/Edit/{id}`; `TermsController.Create/Edit`; view `Areas/Admin/Views/Terms/Form.cshtml`; model `ViewModels/TermFormViewModel.cs`; Admin.

**Current structure**
`h1.h3` "New term" / "Edit term"; form `col-12 col-md-8 col-lg-6`: model-level error summary; Name (placeholder "1st Sem 2026-27"); Start date + End date side by side from `sm` (`<input type="date">` via `[DataType(DataType.Date)]`); buttons "Save" (primary) + "Cancel" (outline secondary → list).

**Current functionality**
- Client validation: "Enter the term name." (max 50), "Pick the start date.", "Pick the end date."
- API 400/409 errors land next to the matching field or in the summary (`AddApiErrors`), e.g. end before start or a duplicate name (exact API messages: **Needs verification**).
- Success → list with "Term {name} saved."

**UI/UX assessment**
- Clean and correct. No breadcrumb or back link apart from Cancel. The page title doesn't say which term is being edited.

**Recommended improvements**
- Add a breadcrumb "Terms › New term" / "Terms › {name}" (same pattern as Class details) for orientation.
- In Edit mode, show the term name in the heading ("Edit term: 1st Sem 2026-27").
- Optional hint under the dates: "The end date must be after the start date." (only if it matches the API rule; **Needs verification**).
- Disable Save while submitting (shared behavior).

**Must preserve**
- `asp-action="@(isNew ? "Create" : "Edit")" asp-route-id="@Model.Id"`, field names `Name`, `StartDate`, `EndDate`.
- `[DataType(DataType.Date)]` rendering of the date inputs (removing it turns them into text boxes; see M5 report line 58).
- `asp-validation-summary="ModelOnly"` + per-field spans; validation scripts section.

---

### P9. Subjects list

**Identity**
- Route: `GET /Admin/Subjects`; `SubjectsController.Index`; view `Areas/Admin/Views/Subjects/Index.cshtml`; model `List<SubjectResponse>`; Admin.
- Purpose: the subject catalog. **No delete by design** (`SubjectsController.cs:9`).

**Current structure**
Header with "New subject"; empty state "No subjects yet. Add the first subject to the catalog."; table: Code (bold) · Title · Units (right-aligned) · Edit button.

**UI/UX assessment** — consistent and simple. No search; fine at the current scale. Row "Edit" buttons lack row context for screen readers.

**Recommended improvements**
- Apply the shared table and row-action conventions (§7); add visually hidden context to "Edit".
- Nothing else is needed.

**Must preserve** — links to `Create` and `Edit/{id}` (`asp-route-id="@subject.SubjectId"`).

---

### P10. New / Edit subject (form)

**Identity**
- Routes: `GET/POST /Admin/Subjects/Create`, `/Admin/Subjects/Edit/{id}`; view `Areas/Admin/Views/Subjects/Form.cshtml`; model `ViewModels/SubjectFormViewModel.cs`; Admin.

**Current structure**
`h1.h3`; form: summary; Code (`col-sm-8`, placeholder "CPE 301") + Units (`col-sm-4`, `min=1 max=10`, default 3); Title; Save / Cancel.

**Current functionality**
- Validation: "Enter the subject code." (max 20), "Enter the subject title." (max 150), "Units must be from 1 to 10."
- API errors (e.g. duplicate code; **Needs verification**) on the field or summary. Success → "Subject {code} saved."

**UI/UX assessment** — fine. Field order Code, Units, Title is slightly unusual (Title is the more important field after Code).

**Recommended improvements**
- Optional order: Code → Title → Units.
- Breadcrumb and heading pattern as in P8.

**Must preserve** — field names `Code`, `Title`, `Units`; actions/route ids; validation spans.

---

### P11. Accounts list

**Identity**
- Route: `GET /Admin/Accounts?role={Admin|Teacher|Student|Parent}`; `AccountsController.Index`; view `Areas/Admin/Views/Accounts/Index.cshtml`; model `List<AccountResponse>`; Admin. An unknown `role` value redirects to the unfiltered list.
- Purpose: all user accounts, filtered by role; create, edit, deactivate, reset a student's phone.

**Current structure**
1. Header row (wraps): `h1.h3` "Accounts" + four create buttons "New teacher" (primary) / "New student" / "New parent" / "New admin" (outline primary, `btn-sm`), in the order Teacher, Student, Parent, Admin.
2. Role filter as `nav nav-pills`: All · Admin · Teacher · Student · Parent (order from `AccountsController.Roles`).
3. Empty state: "No {role} accounts yet."
4. Table: Name · Email · Role · ID no. (student number or employee no. or "—") · Status badge (Active green / Inactive grey) · actions. Inactive rows use `text-body-secondary`.
5. Row actions: "Edit" (`btn-sm btn-outline-secondary`); for students "Reset phone" (`btn-sm btn-link`, confirm); for active accounts other than yourself "Deactivate" (`btn-sm btn-link link-danger`, confirm).

**Current functionality**
- Filter via GET `?role=`.
- Deactivate → POST `Deactivate/{id}?role={current filter}` → flash "Account deactivated. That user can no longer log in." or the API's error (e.g. deactivating yourself).
- Reset phone → POST `ResetDevice/{id}?role=` → flash "Phone reset. The student's next login on the app registers their new phone." "Reset phone" shows for every student; the list doesn't say whether a phone is registered (the DTO has no such field).
- Reactivating an account is only possible through the "Account is active" checkbox on the Edit page.

**UI/UX assessment**
- Four create buttons compete with the page title; the primary emphasis on "New teacher" is arbitrary. Their order (Teacher, Student, Parent, Admin) differs from the filter order (Admin, Teacher, Student, Parent).
- No search: finding one parent among many requires scrolling. (Scale today is small; **Needs verification** of expected account counts.)
- Six columns plus three actions scroll sideways on phones; no column is hidden at small widths (unlike the Teacher tables).
- When a role filter is active, the Role column repeats the same value on every row.
- Mixed action styles: outline button, plain link (blue), danger link.
- Repeated "Edit" / "Deactivate" controls have no row context for screen readers.

**Recommended improvements**
- Replace the four create buttons with **one** primary "New account" control that leads to the role choice. Two options: (a) a Bootstrap dropdown button listing the four roles (each item links to `Create?role=X`); (b) when a role filter is active, show a single "New {role}" primary button and the dropdown otherwise. Both keep the existing `Create?role=` routes.
- Use one role order everywhere (suggest: Teacher, Student, Parent, Admin, the frequency order), or the controller's order. Pick one and apply it to both the pills and the create menu.
- Hide the Role column when a role filter is active.
- Phones: hide Email below `md` (show it under the name in small muted text instead), keep Name, Status and actions visible, following the Teacher-area pattern.
- Optional (feature, ask first): a client-side "Filter by name or email" text box over the table. No API change needed.
- Apply the row-action convention; add visually hidden row context (e.g. "Edit<span class="visually-hidden"> {name}</span>").

**Must preserve**
- `?role=` query parameter on the filter links, create links (`Create?role=`) and the `asp-route-role="@selected"` on Deactivate / ResetDevice forms (so the user returns to the same filter).
- The rule that Deactivate is hidden for the signed-in admin's own row (`a.UserId.ToString() != myUserId`, from the `NameIdentifier` claim).
- `data-confirm` texts or equivalent confirmations for Deactivate and Reset phone.
- POST + anti-forgery on both forms.

---

### P12. New / Edit account (form) and Linked students

**Identity**
- Routes: `GET /Admin/Accounts/Create?role=` (missing/invalid role → list), `POST /Admin/Accounts/Create`, `GET/POST /Admin/Accounts/Edit/{id}`; plus `POST LinkStudent/{id}` and `POST UnlinkStudent/{id}?studentId=` from the links section.
- Views: `Areas/Admin/Views/Accounts/Form.cshtml`, partial `Areas/Admin/Views/Accounts/_ParentLinks.cshtml`.
- Models: `ViewModels/AccountFormViewModel.cs`, `ViewModels/ParentLinksViewModel.cs`.
- Auth: Admin.

**Current structure**
1. `h1.h3` "New {role}" / "Edit {role}".
2. Form (`col-12 col-md-8 col-lg-6`): summary; hidden `Role`; Full name; Email ("Used to log in."); **Create only:** Password ("At least 8 characters. Give it to the user privately.").
3. Role-specific fields:
   - Student: Student number (placeholder "2026-00007") + Year level (1–6); Program (placeholder "BSCpE").
   - Teacher: Employee no. + Department.
   - Parent: Contact email ("Alerts go here. Leave blank to use the login email.") + Contact number (placeholder "+639170000000").
   - Admin: no extra fields.
4. **Edit only:** New password ("Leave blank to keep the current password."); checkbox "Account is active".
5. Save / Cancel (→ list filtered to this role).
6. **Parent Edit only:** `_ParentLinks` section below the main form (`mt-5 col-12 col-lg-10`):
   - `h2.h5` "Linked students" + explanation.
   - Table: Student no. · Name · Relationship ("—" if none) · Receives alerts (Yes/No) · "Unlink" (`link-danger`, confirm "Unlink {name} from this parent?").
   - `h3.h6` "Link a student": a separate form with Student select ("Choose a student…", only active, not-yet-linked students), Relationship text (max 30, placeholder "Mother"), "Receives alerts" checkbox (checked by default), "Link" (`btn-outline-primary`). If no student can be linked: "Every active student is already linked to this parent."

**Current functionality**
- Client validation: full name required (max 150), email required/valid (max 256), password/new password 8–100 characters, year level 1–6, contact email valid, max lengths.
- Create without password → server error "Enter a password (at least 8 characters)." next to Password.
- Which role fields are required is decided by the API; its field errors appear next to the matching input because the view-model property names match the API's (`AccountFormViewModel.cs:5-8`).
- Link without a student → flash "Pick a student to link."; API rejections → error flash; success → "Student linked." / "Student unlinked."
- Success on save → list with "{Role} account for {name} created." / "Account for {name} saved."

**UI/UX assessment**
- The role is shown only in the heading. Nothing else on the form reminds the admin which kind of account they're creating.
- On a parent's Edit page there are **two independent forms**: the main form's Save button sits above the "Link a student" form. Users may think linking needs "Save", or that "Save" also saves the link fields. Link/Unlink take effect immediately and reload the page, while unsaved edits in the main form are lost when they do. That's a real data-loss risk for typed-but-unsaved changes.
- "Account is active" (the only way to reactivate an account) is a small checkbox at the bottom, easy to miss.
- No breadcrumb.

**Recommended improvements**
- Breadcrumb "Accounts › {Role}s › New {role}" / "… › {name}".
- Group the main form with simple headings or fieldsets: "Login" (name, email, password), "{Role} details" (role fields), "Status" (Edit: active checkbox + new password). Use `<fieldset>`/`<legend>` for screen readers.
- Make the account status clearer in Edit: show a status line near the heading ("Active" / "Inactive") and keep the checkbox, with help text "Turn off to stop this user logging in."
- Separate the parent links visually from the main form: clear section break and heading, and a short note "Changes here are saved immediately." Optional: warn before navigating away when the main form has unsaved edits (small `beforeunload` check, shared helper).
- Keep "Link" as the only primary-looking control inside the links section; align the checkbox with the inputs on desktop.

**Must preserve**
- Field names exactly as the view model properties: `Role` (hidden), `FullName`, `Email`, `Password`, `NewPassword`, `IsActive`, `StudentNumber`, `YearLevel`, `Program`, `EmployeeNo`, `Department`, `ContactEmail`, `ContactNumber`. They must match the API's property names for error mapping.
- Showing Password only on Create and NewPassword/IsActive only on Edit.
- The role-switch rendering (`Model.Role == "Student" / "Teacher" / "Parent"`).
- Links form: names `studentId` (select, `required`), `relationship` (max 30), `receivesAlerts` (checkbox with `value="true"`; unchecked posts nothing → `false`). Route `asp-route-id="@Model.ParentUserId"`.
- Unlink form: `asp-route-id="@Model.ParentUserId" asp-route-studentId="@s.StudentId"`, POST, confirm.
- The partial is rendered only when `Model.ParentLinks` is not null; the controller reloads it after a failed Edit (`AccountsController.cs:73`).
- `autocomplete="new-password"` on password inputs, `autocomplete="off"` on name/email.

---

### P13. Classes list

**Identity**
- Route: `GET /Admin/Classes?termId=`; `ClassesController.Index`; view `Areas/Admin/Views/Classes/Index.cshtml`; model `ClassListViewModel`; Admin.
- Default filter: the active term; if none is active, "All terms" (`Services/ClassService.cs:16-17`). `termId=0` = all terms.

**Current structure**
1. Header: `h1.h3` "Classes" + primary "New class".
2. GET filter form: "Term" select (options "All terms", each term, active one suffixed " (active)") + "Show" button (`btn-outline-secondary`).
3. Empty state: "No classes in this term yet."
4. Table: Subject (bold code + muted title) · Section · Teacher · Schedule ("—") · Room ("—") · Enrolled (right) · [Term, only for "All terms"] · actions "Students" (`btn-sm btn-outline-primary`) + "Edit" (`btn-sm btn-outline-secondary`).

**UI/UX assessment**
- Works without JavaScript (good). The separate "Show" button is one extra step; acceptable.
- 7–8 columns scroll sideways on phones; no column hiding.
- The empty-state copy says "in this term" even for "All terms".
- "Students" is the main row action (it opens the class detail page, which also has "Edit class"), and both buttons look nearly equal.

**Recommended improvements**
- Optional progressive enhancement: submit the filter on change (keep the "Show" button for no-JS, or visually hide it only when JS runs).
- Phones: hide Schedule and Room below `md`; consider showing them under the subject in small muted text.
- Make the subject/section cell a link to Details (the row's main destination), and keep "Edit" as the secondary row action. Or keep "Students" but rename it to "Open" / "Students and enrollment" consistently with P15's title.
- Empty state for all terms: "No classes yet."

**Must preserve**
- GET form with `name="termId"` (the controller binds `int? termId`; `0` means all).
- `Model.SelectedTermId == 0` controls the extra Term column.
- Links to `Details/{id}`, `Edit/{id}`, `Create`.

---

### P14. New / Edit class (form)

**Identity**
- Routes: `GET/POST /Admin/Classes/Create`, `/Admin/Classes/Edit/{id}`; view `Areas/Admin/Views/Classes/Form.cshtml`; model `ClassFormViewModel`; Admin.

**Current structure**
`h1.h3`; form: summary; Subject select ("Code – Title"); Section (placeholder "A") + Term select (active term preselected on Create); Teacher select ("Only active teacher accounts are listed."); Schedule (placeholder "MWF 08:00–09:00") + Room (placeholder "CpE 204"); Save / Cancel.

**Current functionality**
- Validation: "Pick a subject.", "Pick a teacher.", "Pick a term.", "Enter the section." (max 30), Schedule max 100, Room max 30.
- After a failed post the dropdown options are rebuilt (`ClassesController.cs:78-80`).
- Known limit: a class whose teacher was deactivated must pick a new teacher on edit (`ClassService.cs:56` shortcut).
- Success → list for that term, "Class section {section} saved."

**UI/UX assessment**
- Field order (Subject, Section, Term, Teacher, Schedule, Room) mixes "what" and "when" fields. Acceptable.
- On Edit, if the current teacher is inactive, the select shows the placeholder with no explanation.

**Recommended improvements**
- Order: Term → Subject → Section → Teacher → Schedule / Room (term first, matching the list filter).
- Breadcrumb "Classes › New class" / "Classes › {code} – {section} › Edit".
- On Edit with no matching teacher option, show help text "The previous teacher's account is inactive. Pick a new teacher." This needs the view to detect that `TeacherId` has a value but no option matches.

**Must preserve** — field names `SubjectId`, `TeacherId`, `TermId`, `Section`, `Schedule`, `Room`; `asp-items` lists; the empty first option in each select (so `[Required]` catches "not picked").

---

### P15. Class details (enrollments)

**Identity**
- Route: `GET /Admin/Classes/Details/{id}`; `ClassesController.Details`; view `Areas/Admin/Views/Classes/Details.cshtml`; model `ClassDetailsViewModel`; Admin.
- Purpose: who is enrolled; enroll and drop students.

**Current structure**
1. Breadcrumb: "Classes" (→ list for this class's term) › "{code} – {section}".
2. Header: `h1.h3` "{code} {title} – {section}"; muted line "Teacher · Term · Schedule · Room"; "Edit class" (`btn-outline-secondary`) on the right.
3. `h2.h5` "Students (N enrolled)".
4. Empty state "No students enrolled yet." or a table: Student no. · Name · Status badge (Active green / Dropped grey; dropped rows muted) · "Drop" (`link-danger`, confirm "Drop {name} from {code} – {section}?").
5. `h3.h6` "Enroll a student": select of active students not actively enrolled ("Name (Student no.)"; dropped students can be re-enrolled) + primary "Enroll". Or: "Every active student is already enrolled in this class."

**Current functionality**
- Enroll → POST `Enroll/{id}` with `studentId`; missing → "Pick a student to enroll."; API error → flash; success → "Student enrolled."
- Drop → confirm → POST `Drop/{id}?enrollmentId=` → "Student dropped. Their attendance and grades are kept."

**UI/UX assessment**
- Clear. Dropped and active students are mixed in one list sorted by the API's order (**Needs verification** of the sort order); with many drops the active roster is harder to read.
- The enroll form is below the roster; with a long class list the admin scrolls to the bottom to add someone.
- The student select becomes long with many students (no search).
- "Enroll" is `btn-primary` here, while the equivalent "Link" on the parent page is `btn-outline-primary`: inconsistent emphasis for the same "inline add" pattern.

**Recommended improvements**
- Put the "Enroll a student" form **above** the roster (or in the section header), since adding is the main task on this page.
- Show active students first; group or collapse dropped students under "Dropped (N)" (pure view logic on `Model.Enrollments`).
- Use one style for inline-add forms across the app (§7).
- Optional (feature, ask first): make the student select searchable. Avoid adding a library; a native `<input list>` datalist can't post the id safely, so this needs care. Leave it unless asked.

**Must preserve**
- Enroll form: `asp-action="Enroll" asp-route-id="@c.ClassId"`, select `name="studentId"` with an empty first option and `required`.
- Drop form: `asp-route-id="@c.ClassId" asp-route-enrollmentId="@e.EnrollmentId"`, POST, confirm.
- Breadcrumb link keeps `asp-route-termId="@c.TermId"`.

---

### P16. Failed emails

**Identity**
- Route: `GET /Admin/FailedEmails`; `FailedEmailsController.Index`; view `Areas/Admin/Views/FailedEmails/Index.cshtml`; model `List<FailedEmailResponse>`; Admin. Read-only.
- Purpose: parent alert emails that Brevo refused or that ran out of retries (newest 100).

**Current structure**
`h1.h3` "Failed emails" + explanation; empty state "No failed emails. Every alert reached Brevo."; table: Created (Manila) · Recipient (name + muted email) · Alert (type, "AtRisk" → "At risk", + muted subject; `.cell-readable`) · Attempts (right) · Last error (small, `text-break cell-readable`). Footer note "Showing the newest 100." when exactly 100 rows.

**UI/UX assessment**
- Recently polished (2026-11-11 report): phones scroll the table inside its box instead of collapsing columns.
- Not reachable from the Admin home list (see P6).
- No next step: the explanation says "Check the address in Accounts", but the recipient isn't linked to their account (the DTO has no user id, so a direct link isn't possible without an API change).
- "Brevo" is a vendor name that admins may not know.

**Recommended improvements**
- Keep the table. Optional copy: "the email service" instead of "Brevo" in user-facing text (ask the team; the name is also used in the docs).
- Make the "Accounts" mention in the intro a link to `/Admin/Accounts?role=Parent`.
- No retry/delete actions (none exist in the API; don't add them).

**Must preserve** — read-only behavior; `ManilaTime.Format`; the `Count == 100` note; `.cell-readable` / `text-break` on long cells.

---

### P17. My classes (teacher)

**Identity**
- Route: `GET /Teacher/Classes`; `Teacher/ClassesController.Index`; view `Areas/Teacher/Views/Classes/Index.cshtml`; model `List<ClassResponse>` (only this teacher's classes, from `api/teacher/classes`); Teacher. Teachers land here after login.

**Current structure**
`h1.h3` "My classes"; empty state "You have no classes yet. Ask the admin to assign you to a class."; table: Subject (bold code + muted title) · Section · Term · Schedule · Room · Enrolled · "Sessions" button (`btn-sm btn-outline-primary`, `aria-label="Sessions for {code} {section}"`).

**UI/UX assessment**
- The button says "Sessions", but the class page it opens also holds assessments and grades. The label undersells the destination.
- No sign of a **live session** in the list. A teacher returning mid-class has to open the class, then find the session.
- All classes from all terms appear together, with Term as a column (**Needs verification**: whether `api/teacher/classes` filters to the active term).
- 7 columns scroll on phones; no column hiding, although teachers may use phones.

**Recommended improvements**
- Make the class name the link (the whole subject cell or row), and drop or rename the separate button to "Open".
- Phones: hide Term, Schedule and Room below `md`; show schedule/room under the subject in small muted text.
- Optional (needs extra API calls per class; ask first): a "Live now" indicator for classes with an open session. Without it, the P18 change below covers the main need.

**Must preserve** — link to `Details/{id}` within the Teacher area; the empty state copy intent; per-teacher data comes from the API (don't add client filtering that hides classes).

---

### P18. Teacher class page

**Identity**
- Route: `GET /Teacher/Classes/Details/{id}`; `Teacher/ClassesController.Details`; view `Areas/Teacher/Views/Classes/Details.cshtml`; model `TeacherClassViewModel`; Teacher. The API returns 403/404 for another teacher's class.
- Also rendered (by explicit path `~/Areas/Teacher/Views/Classes/Details.cshtml`) after a failed `POST /Teacher/Sessions/Start` and a failed `POST /Teacher/Assessments/Create`, with the user's values and errors.
- Purpose: the teacher's hub for one class: start a session, manage assessments, see past sessions.

**Current structure (top to bottom)**
1. Breadcrumb "My classes › {code} – {section}".
2. Header: `h1.h3` "{code} {title} – {section}"; muted "Term · Schedule · Room · N enrolled".
3. **Start a session** card (`_StartSession.cshtml`): `h2.h5`; muted "It starts now and shows a QR code students scan to check in. Times are Manila time."; form: summary, hidden `ClassId`, "Ends at" (`datetime-local`, default now + 90 min, whole minutes), "Late after (minutes)" (default 15, 0–120), primary "Start session".
4. `h2.h5#assessments` "Assessments": "Weights: **X of 100%**"; a yellow warning when the total is under 100% ("The weights add up to X%. Add assessments until they reach 100%; you can enter grades before then."); empty state "No assessments yet. Add the first one below." or a table: Title · Type (hidden below `sm`) · Max score · Weight · Graded ("N of M") · "Grades" button (with visually hidden " for {title}").
5. **Add an assessment** card (`_AssessmentForm.cshtml`, `h3.h6`): summary, hidden `ClassId`, Title (placeholder "e.g. Quiz 1"), Type (free text + datalist Quiz/Exam/Activity/Project/Recitation), Max score (0.01–9999.99), Weight (%) (0.01–100), primary "Add" (`col-md-1`).
6. `h2.h5.mt-5` "Sessions": empty "No sessions yet." or table (newest first): Date ("ddd, MMM d, yyyy") · Time (Manila) "start – end" · status badge (`_SessionStatusBadge`) · "Show QR" (primary, live session) / "View" (outline).

**Current functionality**
- Start session → creates the session starting now, then opens it, then redirects to P19. If opening fails, the session stays Scheduled, P19 shows an "Open session" button and the error flash (`SessionService.cs:38-47`).
- Start validation: "Pick the end time.", "Enter the minutes until Late.", "Use 0 to 120 minutes."; API 400/409 (e.g. end time in the past; **Needs verification** of exact messages) → errors on the re-rendered page.
- Add assessment → success goes straight to P20 with "{title} added. Enter the scores below."; a 409 (title already used) shows next to Title.
- Session statuses shown: Scheduled (blue), Open (green), Ended (grey: open but past its end), Closed/Finalized (grey), Cancelled (red).

**UI/UX assessment**
- **A live session is hard to find.** The "Show QR" button for an open session is in the Sessions table at the **bottom**, below the start form, the assessments table and the add form. The Start session card stays at the top even while a session is live, inviting a second session instead of returning to the current one.
- **Duplicate error summaries:** both forms on this page use `asp-validation-summary="ModelOnly"`, and they share one `ModelState`. When either POST fails with an API error that has no field key (added as a model-level error by `AddApiErrors`), **the same message appears in both the Start session card and the Add assessment card**.
- Double-clicking "Start session" may create two sessions. Nothing disables the button, and the action creates and opens a session on each post. **Needs verification**: whether the API rejects a second open session for the same class.
- The weights warning shows even when there are no assessments ("The weights add up to 0%…") right above the "No assessments yet" empty state: two messages for one fact.
- The page is long and mixes three jobs (attendance now, grading, history). Headings exist, but there are no in-page shortcuts.
- The "Add" button is narrow at `md` (`col-md-1`).

**Recommended improvements**
- **Live session first:** when `Model.Sessions` contains a session with `IsLive`, show a prominent strip directly under the header: "Session in progress · ends 10:30 AM · [Show QR]" (primary). In that state, collapse or de-emphasize the Start session card (e.g. hide the form behind "Start another session" or show a note). All data needed is already in the view model.
- **Fix duplicate summaries:** give each form its own error area. The simplest approach that keeps controllers unchanged: render the model-level summary only in the form that was posted. The view can't tell which one was posted from `ModelState` alone, so the clean fix is a small controller change: the failing action adds model-level errors under a form-specific key, and each partial shows errors for its own key. This touches `SessionsController.Start` and `AssessmentsController.Create`. Flag it to the team as a small behavior-preserving controller change.
- Disable submit buttons after the first submit (shared behavior, §7).
- Show the weights line/warning only when there's at least one assessment; when the total is 100% show a quiet "Weights complete" confirmation.
- Optional: a compact in-page nav under the header (Attendance · Assessments · Sessions) using anchors; `#assessments` already exists.
- Give the Add button room (`col-md-2` or put the button on its own row on `md`).
- Keep the status badge colors (they're the incumbent system; see M9 report).

**Must preserve**
- `_StartSession` form: `asp-controller="Sessions" asp-action="Start" method="post"`, fields `ClassId` (hidden), `EndTime` (`type="datetime-local"`, `asp-format="{0:yyyy-MM-ddTHH:mm}"`), `LateAfterMinutes`.
- `_AssessmentForm`: `asp-controller="Assessments" asp-action="@(isNew ? "Create" : "Edit")" asp-route-id="@Model.AssessmentId"`, fields `ClassId` (hidden), `Title`, `Type`, `MaxScore`, `WeightPercent`, the `assessment-types` datalist. The partial is shared with P20; don't fork it.
- The element `id="assessments"` (P20's breadcrumb links to `#assessments`).
- The view path `Areas/Teacher/Views/Classes/Details.cshtml` (two controllers render it by explicit path) and its model type `TeacherClassViewModel` with `Start` / `NewAssessment` overridden via `with`.
- Session links to `Sessions/Details/{id}`; assessment links to `Assessments/Details/{id}`.
- `_ValidationScriptsPartial` in the Scripts section.

---

### P19. Session page (QR code, live roster, corrections)

**Identity**
- Route: `GET /Teacher/Sessions/Details/{id}`; `Teacher/SessionsController.Details`; view `Areas/Teacher/Views/Sessions/Details.cshtml` + `_Live.cshtml`; models `SessionPageViewModel`, `SessionLiveViewModel`; Teacher (API checks ownership).
- Polled endpoint: `GET /Teacher/Sessions/Live/{id}` (partial, `no-store`).
- Purpose: **the core screen of the product.** The teacher projects or shows the rotating QR; students scan with the app; the roster updates. After the session, the teacher corrects results.

**Current structure**
1. Breadcrumb "My classes › {code} – {section} › {day}".
2. Header: `h1.h3` "{code} – {section}" + status badge; muted "{day} · {start} – {end} (Manila) · Late after N min". Right side: "Open session" (primary; only when Scheduled) or "End session" (`btn-outline-danger`, confirm "End this session? Students who haven't scanned can no longer check in."; shown whenever status is Open, including Open-but-past-end).
3. Body by state:
   - Cancelled, never opened: `alert-secondary` "This session was never opened, so no attendance was taken."
   - Scheduled: muted "This session hasn't been opened yet. Open it to show the QR code."
   - Opened (live or ended):
     - Hidden `#live-status` warning "Reconnecting… The QR and list below may be out of date." (shown on poll failure).
     - Visually hidden `#live-announcer` (`aria-live="polite"`) that receives the summary text.
     - `#live-panel` containing `_Live`.
4. `_Live.cshtml` (one root `div.row.g-4` with `data-closed` and `data-summary`):
   - **Live:** left `col-lg-5`: visually hidden `h2` "Check-in QR code"; `.qr-code` box (`role="img"`, label "Check-in QR code. Students scan it with the Klasly app.") with the server-rendered SVG; muted "Changes every 30 s". Right `col-lg-7`: visually hidden `h2` "Attendance"; bold summary "N of M checked in · P Present · L Late"; roster table (`table-sm`): Student no. (≥ `md`) · Name · Status badge ("Not yet" when null; Present green, Late yellow, Absent red, Excused cyan) · Scanned (Manila) (≥ `md`) · Source (≥ `sm`; "Scan", "Auto", "Corrected"; on phones under the badge).
   - **Ended:** no QR; full-width layout; `alert-secondary` "This session has ended. Students can no longer check in." plus, when correctable, "To fix a result, choose a new status in the **Correct** column and press Save." Summary adds "· A Absent · E Excused".
   - **Correct column** (only when the API status is Closed or Finalized): per row a form with a status select (Present/Late/Absent/Excused; "Choose…" disabled placeholder when no status) and "Save" (with visually hidden " for {name}").
5. Script (only while `IsLive`): every 5 s (`setTimeout` chain, starts 5 s after the previous poll ends) fetch `Live/{id}` with `redirect: 'manual'`, swap `#live-panel` innerHTML, hide the reconnect warning, announce the summary if changed. On an opaque redirect (login expired/access lost) → reload the page. When the new panel says `data-closed="true"` → reload after 3 s (final state, no poller). On error → show the reconnect warning and keep the last QR/roster.

**Current functionality**
- Open → POST `Open/{id}` → "Session opened. Students can scan now." End → POST `Close/{id}` → "Session ended." API 400/409 (e.g. closed in another tab) → error flash on the same page.
- Correct → POST `Correct/{id}?studentId=` with `status` and `currentStatus` → "Attendance updated for {name}." or "No change: {name} is already {status}." or the API's error.
- The QR token never reaches page JavaScript; the SVG is drawn server-side (`SessionService.cs:79-86`).
- The QR itself rotates every 30 s on the API side; the page refreshes every 5 s, so the shown code is at most ~5 s stale.

**UI/UX assessment**
- Strong foundation, polished in M7 and M9 (accessible headings, live region, reconnect warning, phone column hiding).
- **Projection use:** the QR is capped at 440 px and shares the screen with the roster at `lg`. On a classroom projector, students at the back may need a larger code. There's no full-screen/presentation view. This is a usability gap for the core flow, but it's a new feature: ask before building.
- In the Open-but-past-end state ("Ended" badge, status still Open), the page shows "This session has ended…" without the correction hint, and corrections are not available until the session is closed (by "End session" or the Worker, which runs every ~5 min). The copy doesn't explain this, so the teacher may look for the Correct column and not find it.
- Each correction is a separate full-page POST and reload; correcting several students means several round trips and scrolling back each time. Acceptable at class sizes, but slow.
- "End session" (outline danger) sits in the header next to the title. On phones it wraps under the title, close to the QR. It's confirmed by a dialog, so the risk is low.
- The summary line is plain bold text; with a projector, the count of checked-in students is a key number but has no visual weight.

**Recommended improvements**
- Keep the two-column live layout. Give the **checked-in count** more weight (e.g. a larger number "18 / 32 checked in" with the Present/Late breakdown in smaller text), using the same summary data. Keep the visually hidden announcer behavior.
- In the ended-but-not-closed state, add a line: "Press **End session** to close it and correct results." (The End session button is already shown in that state.)
- After a correction, return the teacher to the same row: append a fragment to the redirect (e.g. `#row-{studentId}`) and give each row that `id`. This needs a one-line controller change (`RedirectToAction(..., fragment)`); flag it.
- Optional, ask first: a "Full screen QR" toggle (Fullscreen API on the `.qr-code` element, or a CSS class that enlarges the QR and hides the roster), keeping the white background and quiet zone.
- Don't animate the 5 s swap; avoid layout shift by keeping the roster table structure stable (it already is).

**Must preserve**
- Element ids **`live-panel`, `live-status`, `live-announcer`** and the inline poller script's logic: `fetch(url, { redirect: 'manual', cache: 'no-store' })`, `opaqueredirect` → reload, `data-closed === 'true'` → reload after 3 s, `setTimeout` chain of 5 s.
- `_Live.cshtml` must have **exactly one root element**, and it must carry `data-closed` and `data-summary`, because the script reads `panel.firstElementChild.dataset`.
- The poller only runs when `s.IsLive` (section inside `@if`).
- `Live` stays a GET partial with `[ResponseCache(NoStore = true, Location = None)]`.
- QR rendering: `@Html.Raw(Model.QrSvg)` inside `.qr-code` with `role="img"` + `aria-label`; `.qr-code` keeps **white background, min-width 320 px, max-width 440 px (or larger), border**; don't apply filters, opacity, dark backgrounds or rounded clipping that touch the quiet zone.
- Open/Close forms: POST to `Open` / `Close` with `asp-route-id="@s.SessionId"`; Close keeps its confirmation.
- Correct form per row: `asp-action="Correct" asp-route-id="@Model.SessionId" asp-route-studentId="@r.StudentId"`, fields `status` (select with options exactly `Present`, `Late`, `Absent`, `Excused`) and hidden `currentStatus`; the label `for="status-{studentId}"`.
- `CanCorrect` is never true in the polled partial (corrections only render on full page loads).
- Status → badge class mapping in `_Live.cshtml` and `_SessionStatusBadge.cshtml` (semantics, even if colors are tokenized).

---

### P20. Assessment grade sheet

**Identity**
- Route: `GET /Teacher/Assessments/Details/{id}`; `Teacher/AssessmentsController.Details`; view `Areas/Teacher/Views/Assessments/Details.cshtml` + `_AssessmentForm`; model `AssessmentPageViewModel`; Teacher.
- Purpose: enter and change scores for every enrolled student; edit the assessment.

**Current structure**
1. Breadcrumb "My classes › {code} – {section}" (links to `#assessments`) › "{title}".
2. Header: `h1.h3` title; muted "{type} · {max} points · {weight}% of the grade".
3. `h2.h5` "Grades"; line "**N of M** graded. Leave a score blank if it isn't graded yet. A student's phone is notified when their first grade is saved."
4. Empty state "No students are enrolled in this class." or one form: table (`table-sm`): Student no. (≥ `md`) · Name (+ row-level StudentId error) · Score (0–max): number input (`step 0.01`, `min 0`, `max`, `.score-input` 7.5 rem) + "/ max", per-row validation · Last saved (Manila) (≥ `sm`). Below: form-level error area (`grades` key) and primary "Save grades".
5. `<details class="card">` "Edit assessment" (opens automatically when the edit form has errors): note "The class weights can't go above 100% in total, and the max score can't be lower than a score already saved." + `_AssessmentForm` in edit mode ("Save").

**Current functionality**
- Save grades → one PUT with all rows; blank scores are skipped. Flash "Grades saved: N new, M changed." or "No changes to save."
- API row errors (`grades[i].score`) land on their rows (ModelState keys ignore case); other errors (e.g. 409 race) show above the button. Typed values are kept on error.
- Edit assessment → "{title} updated."; API errors show in the edit form, which opens automatically.

**UI/UX assessment**
- The save button is only at the bottom. With a long class list, the teacher types scores and scrolls to save; there's no indication of unsaved changes and **no warning when leaving with unsaved scores**.
- The page can't show whether the push notification really happens: per commit `9615d2b` the Android app's Firebase grade push is deferred (code commented out). **Needs verification**: if students currently get no push, the sentence "A student's phone is notified…" is inaccurate and should be removed or reworded until push is live.
- Grade entry is keyboard-friendly (native number inputs in DOM order). Enter in any input submits the whole form, which is fine.
- The Edit section at the bottom as a `<details>` card is appropriate (rare task).

**Recommended improvements**
- Sticky action bar at the bottom of the viewport inside the form (`position: sticky; bottom: 0`) holding the graded count and "Save grades", so saving is always reachable. Keep one submit button (don't duplicate the form).
- Unsaved-changes guard: mark the form dirty on input; warn with `beforeunload` when leaving dirty; clear on submit. Small, shared JS helper (`data-warn-unsaved` attribute).
- Show progress as "N of M graded" next to the Save button, updated live as scores are typed (optional; pure client-side count of non-empty inputs).
- Fix or remove the push sentence once verified.
- Keep the Edit assessment section as is; consider moving the weight/max-score rule text next to the relevant fields.

**Must preserve**
- Form: `asp-action="SaveGrades" asp-route-id="@a.AssessmentId" method="post"`.
- Indexed field names: `Grades[i].StudentId` (hidden) and `Grades[i].Score`, rendered with a `for` loop (`asp-for="Grades[i]…"`) so the model binder and the API's `grades[i].score` error keys line up. Don't switch to `foreach` or rename.
- `asp-format="{0:0.##}"` on scores (shows "18", not "18.00"; M10 report).
- `@Html.ValidationMessage($"Grades[{i}].StudentId")`, `asp-validation-for="Grades[i].Score"`, and `@Html.ValidationMessage("grades")`.
- The `editHasErrors` check keys (`Title`, `Type`, `MaxScore`, `WeightPercent`) and the `open` attribute on `<details>`.
- The shared `_AssessmentForm` partial (edit mode posts to `Edit/{id}`).
- Breadcrumb `asp-fragment="assessments"`.

---

### P21. My children (parent)

**Identity**
- Route: `GET /Parent/Children`; `Parent/ChildrenController.Index`; view `Areas/Parent/Views/Children/Index.cshtml`; model `List<LinkedStudentResponse>`; Parent.
- **A parent with exactly one child never sees this page** (redirect to P22).

**Current structure**
`h1.h3` "My children"; empty state "No children are linked to your account yet. Ask the school admin."; `list-group` of children: bold name + muted student number, and a "View" button (`btn-sm btn-outline-primary`, `aria-label="Attendance and grades for {name}"`).

**UI/UX assessment**
- **Accessibility:** the visible label "View" is not part of the accessible name "Attendance and grades for {name}" (WCAG 2.5.3 Label in Name). Voice-control users saying "click View" may fail.
- The list shows no status. A parent with several children must open each one to see if any class is at risk. (Showing status here needs the overview per child, i.e. more API calls. Ask first.)

**Recommended improvements**
- Make each whole list item a link (`list-group-item-action`) with the child's name as the link text; drop the separate "View" button, or keep it with an accessible name that starts with "View" (e.g. "View attendance and grades for {name}").
- Optional (ask first; extra API calls): an at-risk indicator per child.

**Must preserve** — link to `Details/{id}` with `asp-route-id="@c.StudentId"`; the one-child redirect in the controller.

---

### P22. Child overview

**Identity**
- Route: `GET /Parent/Children/Details/{id}`; `Parent/ChildrenController.Details`; view `Areas/Parent/Views/Children/Details.cshtml`; model `ChildOverviewViewModel`; Parent. An unlinked child → API 403 → P2 with the API's reason.
- Purpose: one child's active-term picture: attendance, grades and the at-risk early warning per class. **The most-viewed page on phones** for parents.

**Current structure**
1. Breadcrumb "My children › {name}" **only when the parent has more than one child**.
2. Header: `h1.h3` child's name; muted "{student no.} · {term name}".
3. Child switcher (more than one child): `nav nav-pills gap-2` with bordered pills; current one active.
4. Empty state "No classes this term."
5. One `section.card.mb-4` per class:
   - `h2.h5` "{code} {title (normal weight)}"; muted "{section} · {teacher}".
   - Risk badge (`fs-6`): "At risk" (red), "On track" (green), "Not computed yet" (grey; Worker hasn't run).
   - `dl.row`: Attendance "Present N · Late N · Absent N · Excused N"; Absence rate (percent or "—"); Weighted average (number or "No grades yet").
   - When computed: small muted "This is an early-warning indicator for review, not a final grade. Updated {time}."
   - `<details>` "Attendance records (N)" (`.overview-toggle` tap target): table Session (Manila date/time) · Status (Absent emphasized in `text-danger-emphasis fw-semibold`), newest first, or "No sessions recorded yet."
   - `<details>` "Grades (N)": table Assessment (+ muted type) · Score ("x / max" or muted "Not graded yet") · Weight, or "No assessments yet."

**Current functionality** — read-only. Switch between children. Expand/collapse records. Counts are live; risk fields are the Worker's latest snapshot.

**UI/UX assessment**
- Polished in M13 (tap targets, even pills, Absent emphasis).
- **No overall answer at the top.** A parent must scan every card to learn whether anything needs attention. Cards follow the API's order, so an at-risk class can be third on a phone screen.
- The attendance line packs four numbers into one sentence; hard to compare at a glance.
- The badge says "At risk" but the page never says **why** (high absence rate, low average, or both) or what the thresholds are. The thresholds come from `SystemSettings` (configurable) and are not in the overview DTO. **Needs verification** whether the API exposes them; don't hard-code 20% / 75.
- The three `fs-6` badges are large compared with the heading; the green "On track" badge competes for attention with "At risk".
- Phones: the base font is 14 px below 768 px (`site.css:1-9`), so the small muted lines are ~12 px, which is hard to read for many parents.

**Recommended improvements**
- Add a one-line summary under the header computed in the view from `o.Classes`: e.g. "1 of 4 classes needs attention" (with the at-risk class names linked to their cards via `#class-{id}`), or "All classes on track", or "Not computed yet" when all risk fields are null.
- Order cards with at-risk classes first (pure view `OrderByDescending(c => c.IsAtRisk == true)`; confirm with the team that this ordering is wanted).
- Present attendance as a compact definition row or small inline stat group (four labelled numbers with tabular numerals), with Absent emphasized when > 0. No charts.
- Explain the badge: under "At risk", state which signal triggered it, derived from data already on the card only if the rule is known. Otherwise keep the existing early-warning sentence and link to the school's policy text if the team provides one.
- Tone down "On track" (e.g. success text with a small check-style mark or `text-success-emphasis`, not a filled large badge), keep "At risk" as the only strong-colored element.
- Raise the phone base font size (see §6.6).

**Must preserve**
- Read-only page; the breadcrumb/switcher only when `Model.Children.Count > 1`.
- Switcher links `asp-action="Details" asp-route-id="@c.StudentId"` with `aria-current="page"` on the current child.
- `aria-labelledby` on each section with `id="class-{ClassId}"` headings; table captions (visually hidden).
- `.overview-toggle` tap target size (≥ ~42 px) on `<summary>`.
- `Percent()` / `Number()` formatting (invariant culture) and `ManilaTime.Format`.
- The early-warning disclaimer sentence (it sets expectations; do not remove).
- Three-state risk display: `true` / `false` / `null`.

---

## 6. Application-wide design analysis

### 6.1 Existing design system and visual identity

- **System:** stock Bootstrap 5.3.3, light theme only, system font stack, Bootstrap default colors. Customizations are few and functional (`wwwroot/css/site.css`, 78 lines): tabular numerals, a custom focus ring (`#258cfb` double ring) on buttons/inputs/selects/checkboxes, `.qr-code`, `.cell-readable`, `.score-input`, `.overview-toggle`, hidden empty validation summary.
- **Typography:** headings are consistently `h1.h3` for page titles, `h2.h5` for sections, `h3.h6` for sub-sections. Muted secondary text uses `text-body-secondary` (and sometimes `small`).
- **Color:** Bootstrap primary `#0d6efd` for actions; semantic badges (success/danger/warning/info/secondary). The template file `_Layout.cshtml.css` adds `#0077cc` and `#1b6ec2`, which mostly don't apply (CSS isolation), and the focus ring is `#258cfb`. There are **no CSS custom properties of Klasly's own**; the brand has no color.
- **Identity:** the word "Klasly" in semibold. No logo, no brand color, template favicon (**Needs verification**). The site looks like the ASP.NET template with good manners.

**Recommendation:** define a small token layer at the top of `site.css` on `:root` that overrides Bootstrap's variables (`--bs-primary`, `--bs-primary-rgb`, `--bs-link-color`, `--bs-link-hover-color`, `--bs-border-radius`, `--bs-body-font-size` where appropriate), plus Klasly tokens for the focus ring and status colors. Choose **one** brand primary with accessible contrast (≥ 4.5:1 for text on white) and use it for actions and links only. Keep semantic colors for status. Remove the template colors. Because Bootstrap compiles its button colors into the `.btn-primary` rules, overriding `--bs-primary` alone doesn't recolor buttons: also set the `.btn-primary` variables (`--bs-btn-bg`, `--bs-btn-border-color`, hover/active variants) in one place. Any webfont is optional; the system stack is fine for an operate tool.

### 6.2 Navigation and information architecture

- Role-based areas are clean: each role sees only its own items, and `/` routes by role.
- Depth is shallow (max 3 levels: My classes › Class › Session/Assessment). Breadcrumbs exist on detail pages (Admin Class details, all Teacher detail pages, Parent overview with >1 child) but **not** on Admin forms.
- Gaps: no mobile collapse; Teacher nav loses its active state on Sessions/Assessments pages; Failed emails is missing from the Admin home list; the teacher's live session is buried (P18).

### 6.3 Shared components and reusable patterns

See §7 for the inventory. Patterns are applied consistently by copy-paste (page header row, table with hidden "Actions" header, muted empty-state paragraph, `col-md-8 col-lg-6` form with Save/Cancel, inline-add form under a table, `data-confirm` row forms), but only a few are actual partials.

### 6.4 Design inconsistencies across pages

| Inconsistency | Where |
|---|---|
| Row actions mix `btn-outline-secondary`, `btn-outline-primary`, `btn-link` and `btn-link link-danger` with no rule for which is used when | P7, P11, P13, P15, P12 links table |
| Inline-add submit: `btn-primary` ("Enroll") vs `btn-outline-primary` ("Link") | P15 vs P12 |
| Role order: create buttons Teacher/Student/Parent/Admin vs filter Admin/Teacher/Student/Parent | P11 |
| Status display: Terms show only an "Active" badge (blank otherwise); Accounts show Active/Inactive badges; Enrollments Active/Dropped badges | P7 vs P11 vs P15 |
| Admin tables never hide columns on phones; Teacher tables do | Admin vs Teacher |
| Breadcrumbs on detail pages but not on forms | P8, P10, P12, P14 |
| Message pages (Access denied, Server unavailable, Error) use three different structures; Error is the raw template | P2, P5, P4 |
| 404 responses are the **plain-text** "Status Code: 404; Not Found" page from `UseStatusCodePages()` with no layout (`Program.cs:85`). This happens whenever the API returns 404 (e.g. a mistyped id) | All detail routes |
| Repeated controls ("Edit", "Deactivate", "Drop", "Unlink", "Students") have no row context for screen readers, while Teacher pages do ("Grades for {title}", "Save for {name}") | Admin tables |

### 6.5 Mobile and desktop experience

- **Desktop:** comfortable `.container` widths, readable tables, forms constrained to half width. Good.
- **Mobile:** forms stack correctly (`col-12` first). Tables scroll horizontally inside `.table-responsive`. Teacher and Parent pages were measured at 375/480 px in earlier reports and hide secondary columns. Admin pages don't. The navbar wraps instead of collapsing. The 14 px base font below 768 px makes `small` text ~12.25 px.
- Teachers might run a session from a phone; P19 was tuned for that (QR ≥ 320 px, Correct column kept on screen).

### 6.6 Accessibility and usability

**Already good (keep):** `lang="en"`; labels on every input; `scope="col"` on headers; visually hidden "Actions" headers; `aria-current` on nav, breadcrumbs and pills; live region on the session page; `role="img"` + label on the QR; captions on parent tables; visible custom focus ring; status badges always carry text (never color alone); confirmations on destructive actions.

**Issues found in code:**
1. Base font 14 px on phones (`site.css:1-9`): small/muted text becomes ~12 px. Recommend 16 px everywhere (remove the media query) and re-check table density.
2. Label-in-name mismatch on P21's "View" button.
3. Repeated row-action names without context on Admin tables.
4. No double-submit protection on any POST form; on slow responses users get no feedback (especially Login, Start session, Save grades, Enroll).
5. No unsaved-changes protection on the two forms where losing typed data hurts (grade sheet; parent Edit page with the separate links form).
6. `ApiUnavailable` uses a link as a button.
7. No skip link (minor).
8. Contrast of `text-body-secondary` small text at 12 px: **Needs verification** with a contrast checker after the font-size change.

### 6.7 Opportunities to simplify

- One "New account" control instead of four buttons (P11).
- Teacher class page: show the live session first, de-emphasize Start while live (P18).
- Admin list rows: make the record name the link to its main destination, and leave one or two secondary actions.
- Parent overview: a one-line answer at the top (P22).
- Remove the template leftovers (dead scoped CSS, empty import map, absolute footer, template Error page).

### 6.8 Recommended implementation priorities

Fix the things that mislead or lose data first (Error page, 404 page, double submits, live session visibility, duplicate error summaries), then the shared foundations (tokens, navigation, row actions, message page pattern, font size), then page-level improvements, then polish. Details in §9.

---

## 7. Shared component inventory

### 7.1 Existing partials and shared views

| Component | File | Used by | Notes / what it guarantees |
|---|---|---|---|
| Layout | `Views/Shared/_Layout.cshtml` (+ `_Layout.cshtml.css`, mostly dead) | All pages | Navbar, alerts, footer, scripts |
| Flash alerts | `Views/Shared/_Alerts.cshtml` | Layout | Reads `TempData["Success"]` (`role="status"`) and `TempData["Error"]` (`role="alert"`) |
| Client validation scripts | `Views/Shared/_ValidationScriptsPartial.cshtml` | All form pages | jQuery Validate + Unobtrusive |
| Server unavailable page | `Views/Shared/ApiUnavailable.cshtml` | `ApiExceptionFilter` | 503 page |
| Error page | `Views/Shared/Error.cshtml` | `/Home/Error` (production) | Template content: replace |
| Start session form | `Areas/Teacher/Views/Shared/_StartSession.cshtml` | P18 | Model `StartSessionViewModel` |
| Assessment form (add/edit) | `Areas/Teacher/Views/Shared/_AssessmentForm.cshtml` | P18 (add), P20 (edit) | Mode by `AssessmentId` null/not null |
| Session status badge | `Areas/Teacher/Views/Shared/_SessionStatusBadge.cshtml` | P18, P19 | Uses `SessionResponse.DisplayStatus` |
| Live panel | `Areas/Teacher/Views/Sessions/_Live.cshtml` | P19 + `Live` polling action | Single root with `data-closed` / `data-summary` |
| Parent links | `Areas/Admin/Views/Accounts/_ParentLinks.cshtml` | P12 (parent edit) | Two forms: unlink rows, link a student |

### 7.2 Shared JavaScript and CSS

| Asset | Behavior |
|---|---|
| `wwwroot/js/site.js` | Any `<form data-confirm="…">` asks `window.confirm` before submitting. Used by Deactivate (term, account), Reset phone, Drop, Unlink, End session. |
| Inline script in `Sessions/Details.cshtml` | Live polling (see P19). |
| `wwwroot/css/site.css` | Base font size, focus ring, sticky-footer support, tabular numerals, `.overview-toggle`, validation-summary visibility, `.qr-code`, `.cell-readable`, `.score-input`. |

### 7.3 Repeated markup patterns (not yet components)

| Pattern | Current markup | Pages |
|---|---|---|
| Page header with primary action | `div.d-flex.justify-content-between.align-items-center.mb-3` > `h1.h3.mb-0` + `a.btn.btn-primary` | P7, P9, P11, P13 |
| Detail header with meta line | `h1.h3.mb-1` + `p.text-body-secondary.mb-0` "a · b · c" | P15, P18, P19, P20, P22 |
| Data table | `div.table-responsive > table.table.align-middle`, `th scope="col"`, last header visually hidden "Actions", actions cell `text-end text-nowrap` | Most list pages |
| Empty state | `p.text-body-secondary` one sentence with a next step | All lists |
| Form page | `h1.h3.mb-4` + `form.col-12.col-md-8.col-lg-6`, `asp-validation-summary="ModelOnly"` alert, `mb-3` groups, Save + Cancel | P8, P10, P12, P14 |
| Row action form | `form.d-inline` POST with `data-confirm` + `btn btn-sm btn-link link-danger` | P7, P11, P12, P15 |
| Inline add form | `form.row.g-2.align-items-end` with select + button | P12 links, P15 enroll |
| Status badge maps | inline `switch` → `text-bg-*` | Terms, Accounts, Enrollments, Session status, Attendance status, Risk |

### 7.4 Components to standardize (recommended)

Keep this light: Bootstrap classes plus a few partials. No new libraries or tag-helper frameworks.

1. **Design tokens** (`:root` in `site.css`): brand primary, link colors, focus ring, radii, spacing scale if needed, status colors. One place to change the look.
2. **Row-action convention** (documented rule + consistent markup):
   - The record's main destination = the name/primary cell as a link (or the first action).
   - Secondary actions = `btn btn-sm btn-outline-secondary` (one style for all non-destructive actions).
   - Destructive actions = `btn btn-sm btn-link link-danger` (keep, per the Impeccable decision), always last, always `data-confirm`.
   - Every repeated action has visually hidden row context.
3. **Status badge partial(s):** keep `_SessionStatusBadge`; add one generic `_StatusBadge` (text + semantic variant) used by Terms, Accounts and Enrollments, so "Active / Inactive / Dropped" look identical everywhere. Keep attendance status and risk mappings in their pages if moving them adds coupling.
4. **Message page pattern** for Access denied, Server unavailable, Error, and a new Not found page: same structure (heading, one sentence, one primary action, optional reference).
5. **Page header partial or documented snippet:** title, optional meta line, optional primary action, optional breadcrumb. A documented snippet is enough; make a partial only if it removes real repetition without hiding the markup.
6. **Form submit behavior** in `site.js`: on `submit` (after the existing `data-confirm` check passes), disable the submitter and optionally swap its text to a `data-pending-text` value. Must not run when the confirm dialog was cancelled, and must not break client-side validation (only disable when the form is valid: check `$(form).valid()` when jQuery Validate is present).
7. **Unsaved-changes guard** in `site.js`: `<form data-warn-unsaved>` sets a dirty flag on `input`/`change` and warns on `beforeunload`; cleared on submit. Use on P20 and the P12 main form.

---

## 8. Functional preservation requirements (global)

These apply to every change. Page-specific lists are in each "Must preserve" subsection above.

1. **Routes and actions:** don't rename controllers, actions, areas or route parameters (`id`, `role`, `termId`, `studentId`, `enrollmentId`, `returnUrl`). Two controllers render `~/Areas/Teacher/Views/Classes/Details.cshtml` by explicit path; `ApiExceptionFilter` renders `ApiUnavailable` by name.
2. **HTTP methods:** every state change is a POST form. Keep them POST; never turn a POST action into a GET link.
3. **Anti-forgery:** every POST action has `[ValidateAntiForgeryToken]`. The token is added automatically by the `<form asp-…>` tag helper. **If a form is rewritten without tag helpers (plain `action="…"`), add `@Html.AntiForgeryToken()` or the POST fails with 400.** Keep the `asp-area` / `asp-controller` / `asp-action` / `asp-route-*` attributes.
4. **Model binding names:** input `name`s must match view-model properties (see each page). Indexed names on the grade sheet (`Grades[i].StudentId`, `Grades[i].Score`) must stay index-based. Manual names on the links, enroll and correction forms (`studentId`, `relationship`, `receivesAlerts`, `status`, `currentStatus`) must stay exact.
5. **Error mapping:** view-model property names equal the API's request property names so `AddApiErrors` can place API errors next to inputs. Keep every `asp-validation-for` span and the `asp-validation-summary="ModelOnly"` areas (or an equivalent that renders model-level errors).
6. **Validation:** keep the data-annotation attributes and messages in the view models; keep `_ValidationScriptsPartial` on form pages; keep `novalidate` on the login form only if client validation stays (it disables native bubbles in favor of unobtrusive messages).
7. **Authorization:** keep `[Authorize(Roles = …)]` on every area controller and `[Authorize]` on `HomeController.Index`. Don't add role checks in views that hide data the API already filters, and don't show data the API didn't return.
8. **Business rules stay in the API:** don't add client-side rules that duplicate or contradict API rules (weights ≤ 100%, max score ≥ saved scores, who may correct attendance, at-risk thresholds). Showing the API's message is the rule.
9. **Flash messages:** `TempData["Success"]` / `TempData["Error"]` must keep rendering on every page after redirects.
10. **Confirmations:** keep a confirmation step for Deactivate (term, account), Reset phone, Drop, Unlink and End session (the native `data-confirm` mechanism or an accessible replacement with the same texts).
11. **Live session contract:** ids `live-panel`, `live-status`, `live-announcer`; one root element in `_Live` with `data-closed` and `data-summary`; `Live` stays GET + no-store; the poller's manual-redirect handling.
12. **QR readability:** black on white, quiet zone kept, ≥ 320 px wide, no CSS that alters the SVG's colors or clips it.
13. **Times:** keep `ManilaTime` for display and input conversion; never format UTC values directly.
14. **Security headers / cookies / Program.cs:** don't touch authentication, cookie options, the bearer handler or the API client setup. The one recommended `Program.cs` change (a styled status-code page; §9 P1-2) must keep `UseStatusCodePages` semantics for non-browser callers and needs team approval.
15. **No new packages** without the team's approval (CLAUDE.md §4). Everything in this document can be done with Bootstrap 5.3.3, the existing jQuery Validation and plain JavaScript.

---

## 9. Prioritized UI/UX roadmap

Each item lists the files involved. Items marked **(ask)** add a feature, an API call, or a controller/Program change: confirm with the team first.

### Priority 1: Critical (misleading content, data loss or duplicate records, broken core flow)

| # | Change | Files | Why |
|---|---|---|---|
| P1-1 | Replace the template **Error** page with a user-facing message page (keep Request ID as a small "Reference") | `Views/Shared/Error.cshtml` | Production users currently see developer instructions (P4) |
| P1-2 | **Styled Not found page** instead of the plain-text status page. E.g. `app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}")` + a small action/view using the message-page pattern **(ask: Program.cs + HomeController change)** | `Program.cs:85`, `Controllers/HomeController.cs`, new view | Any API 404 shows an unstyled text page with no navigation (§6.4) |
| P1-3 | **Double-submit protection** on all POST forms (shared `site.js` behavior, §7.4-6) | `wwwroot/js/site.js` | A double click on "Start session" may create two sessions (**verify** API behavior); repeated Enroll/Link/Save posts; no feedback on slow API |
| P1-4 | **Live session first** on the teacher class page; de-emphasize Start while a session is live | `Areas/Teacher/Views/Classes/Details.cshtml`, `_StartSession.cshtml` | The core classroom flow hides the live QR at the bottom (P18) |
| P1-5 | **Separate error summaries** for the two forms on the teacher class page **(ask: small controller change)** | `_StartSession.cshtml`, `_AssessmentForm.cshtml`, `SessionsController.Start`, `AssessmentsController.Create` | The same error appears in both forms (P18) |
| P1-6 | **Unsaved-changes guard + sticky Save** on the grade sheet | `Areas/Teacher/Views/Assessments/Details.cshtml`, `site.js`, `site.css` | Teachers can lose a class's worth of typed scores (P20) |

### Priority 2: High impact (consistency, navigation, readability)

| # | Change | Files |
|---|---|---|
| P2-1 | **Design tokens** on `:root`; one brand primary; remove dead template CSS (`_Layout.cshtml.css`) and the empty import map | `site.css`, `_Layout.cshtml`, `_Layout.cshtml.css` |
| P2-2 | **Navbar:** collapse on phones; area-aware active state (Teacher Sessions/Assessments → My classes); skip link; remove or normalize the absolute footer | `_Layout.cshtml`, `site.css` |
| P2-3 | **Phone font size** to 16 px base; re-check table density and `small` text contrast | `site.css` |
| P2-4 | **Message page pattern** for Access denied (correct copy for role refusals) and Server unavailable (user-facing copy, real button) | `Views/Account/AccessDenied.cshtml`, `Views/Shared/ApiUnavailable.cshtml` |
| P2-5 | **Row-action convention** + row context for screen readers across Admin tables; consistent inline-add button style | `Areas/Admin/Views/*/Index.cshtml`, `Accounts/_ParentLinks.cshtml`, `Classes/Details.cshtml` |
| P2-6 | **Shared status badge** (Active/Inactive/Dropped) and an "Inactive" value on Terms | `Terms/Index.cshtml`, `Accounts/Index.cshtml`, `Classes/Details.cshtml`, new partial |
| P2-7 | **Accounts list:** one "New account" control, one role order, hide Role column when filtered, phone column hiding | `Areas/Admin/Views/Accounts/Index.cshtml` |
| P2-8 | **Admin forms:** breadcrumbs, record name in Edit headings, fieldsets on the account form, clear separation + "saved immediately" note for parent links | `Terms/Form`, `Subjects/Form`, `Accounts/Form`, `Accounts/_ParentLinks`, `Classes/Form` |
| P2-9 | **Parent overview:** summary line, at-risk first (confirm), scannable attendance numbers, calmer "On track" | `Areas/Parent/Views/Children/Details.cshtml`, `site.css` |
| P2-10 | **Parent list:** whole-row links / fix label-in-name | `Areas/Parent/Views/Children/Index.cshtml` |
| P2-11 | **Admin home:** add Failed emails | `Areas/Admin/Views/Home/Index.cshtml` |
| P2-12 | **Class details (admin):** enroll form above the roster; active students first, dropped grouped | `Areas/Admin/Views/Classes/Details.cshtml` |
| P2-13 | **Teacher lists:** class name as the link, phone column hiding, button label matching destination | `Areas/Teacher/Views/Classes/Index.cshtml`, `Areas/Admin/Views/Classes/Index.cshtml` |
| P2-14 | **Grade sheet copy:** verify the push-notification sentence; reword or remove | `Areas/Teacher/Views/Assessments/Details.cshtml` |

### Priority 3: Polish

| # | Change | Files |
|---|---|---|
| P3-1 | Login brand moment (wordmark + one line), alert inside the form column, optional show-password | `Views/Account/Login.cshtml` |
| P3-2 | Brand favicon (**verify** current icon) | `wwwroot/favicon.ico` |
| P3-3 | Session page: larger checked-in count; "Press End session to correct" hint in the ended-but-open state; return to the corrected row via fragment **(ask: one-line controller change)** | `Sessions/Details.cshtml`, `_Live.cshtml`, `SessionsController.Correct` |
| P3-4 | Teacher class page: weights line only when assessments exist; "Weights complete" state; wider Add button; optional in-page nav | `Areas/Teacher/Views/Classes/Details.cshtml`, `_AssessmentForm.cshtml` |
| P3-5 | Terms: confirm on "Make active" naming the term being replaced | `Areas/Admin/Views/Terms/Index.cshtml` |
| P3-6 | Classes list: filter submits on change (keep the button for no-JS); empty-state copy for "All terms" | `Areas/Admin/Views/Classes/Index.cshtml` |
| P3-7 | Class form field order (Term first); help text when the saved teacher is inactive | `Areas/Admin/Views/Classes/Form.cshtml` |
| P3-8 | Failed emails: link "Accounts" in the intro; vendor-neutral wording (ask) | `Areas/Admin/Views/FailedEmails/Index.cshtml` |
| P3-9 | Dismissible success alerts | `Views/Shared/_Alerts.cshtml` |

### Optional features: only if the team asks

Full-screen / projector QR view (P19); accounts search box (P11); searchable student pickers (P12, P15); active-term status on Admin home (P6); live-session or at-risk indicators on list pages (P17, P21); dark mode (not requested; the QR must stay white if it's ever added).

### Suggested implementation order (grouped)

1. **Foundation:** P2-1 tokens, P2-3 font size, P2-2 navbar, P1-3 submit behavior, §7.4 helpers (submit, unsaved guard). Verify every page once after this step, since it touches all of them.
2. **Message pages:** P1-1, P1-2 (ask), P2-4.
3. **Teacher flow (highest value):** P1-4, P1-5 (ask), P1-6, P2-13 (teacher part), P2-14, P3-3, P3-4.
4. **Parent flow:** P2-9, P2-10.
5. **Admin:** P2-5, P2-6, P2-7, P2-8, P2-11, P2-12, P2-13 (admin part), P3-5 to P3-8.
6. **Polish:** P3-1, P3-2, P3-9.

---

## 10. Instructions for the next AI agent

1. **Read this whole document first**, then `CLAUDE.md` (root), especially §3.4 (Web MVC rules), §4 (build/report rules) and §5 (the reports you must write after every code change).
2. **Find the design direction.** No Impeccable `PRODUCT.md` / `DESIGN.md` exists in the repo (§3). Ask the team whether they have one outside the repo. If not, run `/impeccable` to create them, starting from §3 of this document and the Teacher area as the reference, and get the team's approval before restyling. Run `/impeccable` on every view you change before calling the task done (CLAUDE.md §3.4).
3. **Understand before editing.** For each page, read its section here, then the controller action, view, partials and view model listed in its Identity block. Confirm the "Must preserve" items still match the code (the code may have moved on since 2026-11-20).
4. **Preserve behavior.** Change presentation, not workflows. Follow §8 strictly: routes, POST methods, anti-forgery tokens, field names, validation, authorization, the live-session contract and QR readability. Items marked **(ask)** need team approval because they touch controllers, `Program.cs` or add features.
5. **Reuse and standardize.** Implement the foundation (§9, step 1) and the shared components in §7.4 before page work, then apply them everywhere. Don't fork `_AssessmentForm` or `_Live`.
6. **Avoid generic AI UI patterns.** No gradients, decorative blobs, hero sections, glass effects, emoji icons, oversized rounded cards, KPI tiles without a decision behind them, or icon fonts added only for decoration. Every visual change must make a task faster, clearer or more trustworthy. Stay within Bootstrap 5.3.3; no new packages without approval.
7. **Work incrementally.** One roadmap group per change set. Keep diffs small and reviewable. Commit only when the user asks (and never add Claude attribution to commits; see the user's memory note).
8. **Verify every affected page** in a browser at 375 px, 768 px and 1280 px: start the `api` and `web` configurations (`.claude/launch.json`), log in with the test accounts in `docs/TEST-ACCOUNTS.md` (Admin, Teacher, Parent), and exercise each form's success and error paths. For the session page: start a session, watch the 5 s refresh, stop the API to see "Reconnecting…", end the session, correct a row. Check keyboard focus, screen-reader names of row actions and the live region, and the console for errors.
9. **Build and test:** `dotnet build Klasly.slnx` and `dotnet test` after every change (tests need LocalDB). There are **no automated UI tests** for the Web project (nothing in `tests/` references `Klasly.Web`), so the browser check in step 8 is the only safety net for views.
10. **Report honestly** (CLAUDE.md §5): write the three report files under `docs/reports/<yyyy-MM-dd-HHmm>-<slug>/`, append to `docs/reports/INDEX.md`, list what was changed, what was verified (with the actual results), what was not verified, and remaining issues from this roadmap.

---

## 11. Audit coverage check and limitations

### 11.1 Razor files accounted for (all 38 `.cshtml` files in `src/Klasly.Web`)

| File | Covered in |
|---|---|
| `Views/_ViewImports.cshtml`, `Views/_ViewStart.cshtml` | §2.3 |
| `Views/Shared/_Layout.cshtml` (+ `_Layout.cshtml.css`) | P0 |
| `Views/Shared/_Alerts.cshtml` | P0, §7 |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | §7 |
| `Views/Shared/ApiUnavailable.cshtml` | P5 |
| `Views/Shared/Error.cshtml` | P4 |
| `Views/Account/Login.cshtml` | P1 |
| `Views/Account/AccessDenied.cshtml` | P2 |
| `Views/Home/Index.cshtml` | P3 |
| `Areas/Admin/Views/_ViewImports.cshtml`, `_ViewStart.cshtml` | §2.3 |
| `Areas/Admin/Views/Home/Index.cshtml` | P6 |
| `Areas/Admin/Views/Terms/Index.cshtml`, `Form.cshtml` | P7, P8 |
| `Areas/Admin/Views/Subjects/Index.cshtml`, `Form.cshtml` | P9, P10 |
| `Areas/Admin/Views/Accounts/Index.cshtml`, `Form.cshtml`, `_ParentLinks.cshtml` | P11, P12 |
| `Areas/Admin/Views/Classes/Index.cshtml`, `Form.cshtml`, `Details.cshtml` | P13, P14, P15 |
| `Areas/Admin/Views/FailedEmails/Index.cshtml` | P16 |
| `Areas/Teacher/Views/_ViewImports.cshtml`, `_ViewStart.cshtml` | §2.3 |
| `Areas/Teacher/Views/Classes/Index.cshtml`, `Details.cshtml` | P17, P18 |
| `Areas/Teacher/Views/Sessions/Details.cshtml`, `_Live.cshtml` | P19 |
| `Areas/Teacher/Views/Assessments/Details.cshtml` | P20 |
| `Areas/Teacher/Views/Shared/_StartSession.cshtml`, `_AssessmentForm.cshtml`, `_SessionStatusBadge.cshtml` | P18, P19, P20, §7 |
| `Areas/Parent/Views/_ViewImports.cshtml`, `_ViewStart.cshtml` | §2.3 |
| `Areas/Parent/Views/Children/Index.cshtml`, `Details.cshtml` | P21, P22 |

### 11.2 Controller actions mapped (all actions in the 12 Web controllers)

- `AccountController`: Login (GET, POST), Logout (POST), AccessDenied → P1, §4.2, P2.
- `HomeController`: Index, Error → P3, P4.
- `Admin/HomeController`: Index → P6.
- `Admin/TermsController`: Index, Create (GET, POST), Edit (GET, POST), Activate, Deactivate → P7, P8, §4.2.
- `Admin/SubjectsController`: Index, Create (GET, POST), Edit (GET, POST) → P9, P10.
- `Admin/AccountsController`: Index, Create (GET, POST), Edit (GET, POST), Deactivate, ResetDevice, LinkStudent, UnlinkStudent → P11, P12, §4.2.
- `Admin/ClassesController`: Index, Create (GET, POST), Edit (GET, POST), Details, Enroll, Drop → P13–P15, §4.2.
- `Admin/FailedEmailsController`: Index → P16.
- `Teacher/ClassesController`: Index, Details → P17, P18.
- `Teacher/SessionsController`: Details, Live, Start, Open, Close, Correct → P19, P18, §4.2.
- `Teacher/AssessmentsController`: Details, Create, Edit, SaveGrades → P20, P18, §4.2.
- `Parent/ChildrenController`: Index, Details → P21, P22.

### 11.3 Confirmations

- Every discovered Razor file is accounted for (§11.1).
- Every controller action is mapped (§11.2).
- Shared layout and partials are documented (P0, §7).
- Role-specific pages and workflows are covered: Admin (P6–P16), Teacher (P17–P20, including the polled `Live` partial and post-error re-renders), Parent (P21–P22, including the one-child redirect), the student-refusal login path (P1, P3), and the error funnel (P2, P4, P5, §6.4).
- Functional preservation requirements are explicit per page and globally (§8).
- Recommendations are tied to specific files and lines found in the code; proposals are labelled as such.

### 11.4 Limitations of this audit

- **No rendered check.** The pages were not opened in a browser and no screenshots or measurements were taken in this audit. Rendering claims rely on the code and on earlier measured reports. Items marked **Needs verification**: the brand link color from the scoped CSS, the favicon, `text-body-secondary` contrast, API sort orders, API error message texts, whether `api/teacher/classes` filters by term, whether the API blocks a second open session per class, and whether the grade push currently reaches students.
- **The API was read only where the Web depends on it** (endpoint list in `Repositories/`). API-side validation messages and rules were not re-audited.
- **No Impeccable design files exist** in the repo; the direction in §3 is reconstructed from the team's reports.
- **No builds or tests were run** for this audit, because no code was changed.
- The Android app is out of scope (it's not part of the MVC website).
