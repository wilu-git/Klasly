# Tasks: Failed emails page checks (`/impeccable` pass + browser check)

> Plan: [`plan-failed-emails-checks.md`](./plan-failed-emails-checks.md) · Spec: [`SPEC.md`](../SPEC.md). Web MVC only.
> Run: `preview_start "api"` and then `preview_start "web"` (`https://localhost:7144`). LocalDB must be running.
> Log in with the seeded fictional admin and teacher accounts. Change no dev data.

---

## Task 1: Browser check (admin list, teacher refused, API-down error)

**Description:** Confirm the page behaves correctly for each role before touching it. Read-only.

**Done when:**
- [x] As **admin**, `/Admin/FailedEmails` lists the seeded Failed rows: Manila time, recipient name + email, alert + subject, attempts, last error. Screenshot saved.
- [x] As a **teacher**, typing `/Admin/FailedEmails` in the address bar shows the Access denied page and no email data. Screenshot saved.
- [x] With the `api` server stopped, the admin page shows a clear error, not a crash or a blank table. Screenshot saved, then the API is started again.
  - **Correction:** an unreachable API doesn't go through `_Alerts`. The shared `ApiExceptionFilter` shows the house **"Klasly can't reach the server right now."** page (`ApiUnavailable`, HTTP 503, with a Try again button). `_Alerts` is for error *replies* from a running API. This is the intended behaviour, so nothing needs fixing.
- [x] `read_console_messages` shows no errors from the page. With the API up there were none. With the API down, the only entry is the browser noting the page's own 503 status.

**Verification:**
- [x] Manual in the browser pane (2026-11-11, seeded admin `admin@klasly.test` and teacher `rosa.villanueva@klasly.test`). Screenshots are in `docs/reports/2026-11-11-1605-failed-emails-page-checks/`: `1-admin-list.jpg`, `2-teacher-refused.jpg`, `3-api-down.jpg`. A signed-out visit also redirects to the login page with `ReturnUrl=/Admin/FailedEmails`.

**Dependencies:** None
**Files touched:** none (screenshots only)
**Scope:** XS

---

## Task 2: `/impeccable` measurement and defect list

**Description:** Run a narrow `/impeccable polish` review of the view against the house style (Terms / Accounts pages). Measure only, change nothing.

**Done when:**
- [x] At 375, 480 and 1280 px:
  - the page has no sideways scroll (`document.documentElement.scrollWidth <= clientWidth`);
  - the last error and recipient email wrap inside their cells.

  The page never scrolls sideways, but the wrapping is defect **D1** below. The email deliberately doesn't wrap (see "Not defects").
- [x] Contrast measured: body text and headers 21:1, `text-body-secondary` (intro, email, subject) ≈ 6.7:1. All pass AA.
- [x] Structure checked: one `h1`, every `<th>` has `scope="col"`, the nav item has `aria-current`, `lang="en"`, title "Failed emails - Klasly". The "—" for no error can't happen in practice: the Worker always sets `LastError` when it marks a row Failed (`EmailSender.cs:60`, `:79`).
- [x] Empty-state markup read: `<p class="text-body-secondary">No failed emails. Every alert reached Brevo.</p>`, the same pattern as Terms. Not rendered (spec question 1).
- [x] `impeccable detect --json` on the view → `[]`.
- [x] Defect list written (below).

**Measurements** (live page, 100 rows, logged in as the seeded admin):

| Width | Page scrolls sideways | Table / box | Column widths (Created · Recipient · Alert · Attempts · Last error) | First row | "Brevo is not configured" | ~200-char Brevo error (injected in the DOM, then restored) |
|---|---|---|---|---|---|---|
| 375 px | no | 483 / 354 px, scrolls inside | 119 · 167 · **73** · 75 · **49** | 128 px | 4 lines in 35 px | **39 lines, 732 px row** |
| 480 px | no | 483 / 459 px, scrolls inside | same as 375 | 128 px | 4 lines | **39 lines, 732 px row** |
| 1280 px | no | 1116 / 1116 px | 162 · 235 · 420 · 105 · 194 | 62 px | 1 line | 3 lines, 83 px row |

**Defects:**
- **D1: narrow-screen column squeeze (local defect).** Below about 500 px the table shrinks to its smallest possible width. The Last error cell's `text-break` lets it collapse to 49 px, so the column the admin came for breaks every few characters. A realistic Brevo error becomes a 732 px-tall row (39 lines). The Alert subject is squeezed to 73 px and wraps to 5 lines. **Fix in Task 3:** give the Last error and Alert cells a sensible minimum width. Then the table scrolls inside `table-responsive`, which is the house pattern, instead of collapsing. **Target:** the long error at 375 px drops from 39 lines to ≤ 10, and the 1280 px layout is unchanged.

**Not defects (kept):**
- The recipient email doesn't wrap: it holds 167 px. Breaking an address mid-word makes it harder to read and copy, and the table scrolls inside its box anyway. The spec's "email wraps" wording is corrected to "stays whole and readable".
- The Attempts column is 105 px wide at 1280 px for one digit. That's the header text width and matches the other tables.

**Verification:**
- [x] DOM measurements and detector output recorded above. A phone screenshot from before the fix is saved for the report.

**Dependencies:** Task 1
**Files touched:** none
**Scope:** XS

---

## Checkpoint A (after Tasks 1–2)
- [x] The teacher was refused. **If not:** stop, report it as a security finding, and wait for the team.
- [x] The defect list exists (1 defect: D1). **If it says "none found":** skip Task 3 and go to Task 4.

---

## Task 3: Fix the defects and measure again

**Description:** Fix only the defects from Task 2, with Bootstrap utilities first. Keep the Terms table pattern.

**Done when:**
- [x] Every defect from Task 2 is fixed, with a before → after number for each.
  - **D1 fix:** a new `.cell-readable { min-width: 12rem; }` rule in `site.css` (168 px on phones, where the root font is 14 px), on the Alert and Last error cells. `text-break` stays, so an unbroken token still can't overflow.

  | Measure (first row unless noted) | 375 px before → after | 480 px before → after | 1280 px before → after |
  |---|---|---|---|
  | ~200-char Brevo error | **39 → 9 lines**, row 732 → 180 px | 39 → 9 lines, 732 → 180 px | 3 → 3 lines, 83 → 83 px |
  | "Brevo is not configured" | 4 → 1 line | 4 → 1 line | 1 → 1 line |
  | Alert subject | 5 → 2 lines | 5 → 2 lines | 1 → 1 line |
  | Row height | 128 → 73 px | 128 → 73 px | 62 → 62 px |
  | Column widths | 119·167·**73**·75·**49** → 119·167·168·75·168 | same as 375 | 162·235·420·105·194 → 156·227·405·102·227 |
  | Table / box | 483 / 354 → 697 / 354 (scrolls inside) | 483 / 459 → 697 / 459 | 1116 / 1116 → same |
  | Page scrolls sideways | no → no | no → no | no → no |

  At 1280 px the rows are unchanged (same heights, all one line). The browser hands out the spare width a little differently: Last error gains 33 px, and the other columns give up 3–15 px each. This is accepted, not hidden. Keeping desktop pixel-identical would need a media query for no visible gain.
- [x] Every Task 2 check run again at 375 / 480 / 1280 px and passes. Contrast and structure are unchanged: one class added, no colour or markup changed.
- [x] `impeccable detect --json` on the view and `site.css` → `[]`.
- [x] Admin page screenshot taken again after the fix: `5-phone-375-after.jpg` (before: `4-phone-375-before.jpg`).

**Verification:**
- [x] `dotnet build Klasly.slnx` → 0 warnings, 0 errors. The first try hit file locks from the running API and website, so I stopped both servers, rebuilt, and started them again.
- [x] Manual: page reloaded in the browser pane, and every request on the load returned 200. The one 503 in the console log is left over from Task 1's API-down test.

**Dependencies:** Task 2 (only if it found defects)
**Files touched:**
- `src/Klasly.Web/Areas/Admin/Views/FailedEmails/Index.cshtml`
- `src/Klasly.Web/wwwroot/css/site.css` (only if no utility class works)

**Scope:** S

---

## Task 4: Wrap-up (build, tests, ticks, reports, graph)

**Description:** Prove nothing else broke and record the evidence.

**Done when:**
- [x] `dotnet build Klasly.slnx` is green (0 warnings, 0 errors) and `dotnet test Klasly.slnx` is 162/162.
- [x] `tasks/todo-m14.md` Task 8: both open boxes ticked (`/impeccable` pass, browser check).
- [x] Every success criterion in `SPEC.md` is ticked. The not-done items (empty state not rendered, no real Brevo error text) are in the report's Known limits.
- [x] `docs/reports/2026-11-11-1605-failed-emails-page-checks/` has the three files plus 5 screenshots. One line added to `docs/reports/INDEX.md`.
- [x] `graphify update .` has been run.

**Verification:**
- [x] The command output is written in `DEVELOPER-DOCS.md` → "How it was verified".

**Dependencies:** Task 3 (or Task 2 if Task 3 was skipped)
**Files touched:** `tasks/todo-m14.md`, `SPEC.md`, the report folder, `docs/reports/INDEX.md`
**Scope:** S

---

## Checkpoint: Complete
- [x] All spec success criteria are ticked, with the evidence in the report.
- [x] Ready for the team's review (committed on `dev`).
