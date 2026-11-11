# Spec: Failed emails page checks (`/impeccable` pass + browser check)

> Status: **Approved** (2026-11-11). Open questions 1–3 answered as recommended.
> Plan: `tasks/plan-failed-emails-checks.md` + `tasks/todo-failed-emails-checks.md`.
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` → `CLAUDE.md` (§3.4 Web MVC, §4) → this spec.
> Milestone 15's spec is in git history (commit `6522027`) and in `docs/reports/2026-11-11-1402-milestone-15-review/`.

## Objective

Close the two boxes Milestone 14 Task 8 left open in `tasks/todo-m14.md`:

1. `[ ] /impeccable pass on the view.`
2. `[ ] Manual in the browser pane: as admin, the page lists a seeded Failed row; as a teacher, the URL is refused.`

The page itself (`Admin › Failed emails`, commit `26a9362`) already works. The admin half of the browser check was done in M14 (`admin-failed-emails.jpg`), but the teacher half was not. This task is **checking and polishing only**. It adds no features.

**User:** the Admin, who reads this page to find parent emails that never arrived and fix the address in Accounts.

## What exists today

- `src/Klasly.Web/Areas/Admin/Views/FailedEmails/Index.cshtml`: heading + one-line help text, an empty state, a `table-responsive` table (Created · Recipient · Alert · Attempts · Last error), and a "Showing the newest 100." note. It copies the Terms page's table pattern.
- `src/Klasly.Web/Areas/Admin/Controllers/FailedEmailsController.cs`: `[Area("Admin"), Authorize(Roles = "Admin")]`, a single `Index` action.
- The dev database holds the M12 "Brevo is not configured" Failed rows, so the list is not empty.
- No `PRODUCT.md` / `DESIGN.md`. As in M13 Task 5, the existing Admin pages (Terms, Accounts) set the house style.

## Assumptions (correct these before we start)

1. **This is a narrow `/impeccable polish` pass, not a redesign.** It stays on Bootstrap and the Terms table pattern, and fixes only real defects (overflow, wrapping, contrast, tap targets, heading order). No new colours, components or layout.
2. **The spec replaces `SPEC.md`**, the same way each milestone did. The M15 spec stays in git history.
3. **The browser check uses the seeded fictional accounts** (admin + one teacher) on the dev database through the `api` and `web` entries in `.claude/launch.json`. No dev data is changed.
4. **No API, database, Worker or Android change.** If the pass finds that the API should return something different, it gets logged, not fixed here.

## Commands

```
Build:    dotnet build Klasly.slnx
Test:     dotnet test Klasly.slnx
Detector: npx impeccable detect --json src/Klasly.Web/Areas/Admin/Views/FailedEmails/Index.cshtml
Run:      preview_start "api", then preview_start "web" (https://localhost:7144)
Graph:    graphify update .
```

## Project structure (files in scope)

```
src/Klasly.Web/Areas/Admin/Views/FailedEmails/Index.cshtml   ← the only view under review
src/Klasly.Web/wwwroot/css/site.css                          ← only if a fix needs one small rule
tasks/todo-m14.md                                            ← tick the two boxes
docs/reports/<yyyy-MM-dd-HHmm>-failed-emails-page-checks/    ← 3 report files + screenshots
docs/reports/INDEX.md                                        ← one line
```

## Code style

Same as the M13 Task 5 polish: Bootstrap utility classes first, and a `site.css` rule only when no utility class does the job. Colour is never the only signal.

```html
<td class="small text-break">@(email.LastError ?? "—")</td>
```

## Testing strategy

- No new automated tests: the API's 403 for non-admins is already covered by the role-access sweep, and `FailedEmailsTests` covers the data.
- Checks are manual in the browser pane, with DOM measurements and screenshots recorded in the report (the M13 Task 5 method).
- `dotnet build` + `dotnet test` must stay green (currently 162/162).

## Boundaries

- **Always:** keep the Terms table pattern; measure before and after; report what was not checked.
- **Ask first:** changing dev data (for example, to show the empty state), touching any other page, or adding a package.
- **Never:** change the API, the database or the Worker; enter real people's data; commit secrets.

## Success criteria

**`/impeccable` pass**
- [ ] Rendered at 375, 480 and 1280 px: the page itself never scrolls sideways (the table may scroll inside `table-responsive`, and the Last error and recipient text wrap instead of stretching it).
- [ ] Text and secondary text contrast meet WCAG AA (4.5:1) in the light theme.
- [ ] Heading order is correct (one `h1`), table headers use `scope="col"`, and the "—" for no error is announced sensibly or is fine as-is.
- [ ] `impeccable detect --json` on the view returns `[]`.
- [ ] Every defect found is fixed in the view (or `site.css`), with before/after numbers in the report. If none is found, the report says so and no file changes.

**Browser check**
- [ ] As **admin**: `/Admin/FailedEmails` lists the seeded Failed rows with Manila time, recipient and last error (screenshot).
- [ ] As a **teacher**: opening `/Admin/FailedEmails` directly is refused (Access denied page, no data shown; screenshot).
- [ ] No console errors from the page.

**Wrap-up**
- [ ] `dotnet build` green, `dotnet test` 162/162.
- [ ] Both boxes ticked in `tasks/todo-m14.md`; report folder + `INDEX.md` line written; `graphify update .` run.

## Open questions

1. **Empty state:** the dev database has Failed rows, so "No failed emails. Every alert reached Brevo." can't be seen without changing data. **Recommend: don't change data; check the empty-state markup by reading it and note it in the report as not rendered.**
2. **API-down error:** should we also stop the API and confirm the page shows the error through `_Alerts`? It was ticked in M14 by the shared `ApiExceptionFilter`, not seen on this page. **Recommend: yes. It's one extra screenshot.**
3. **Parent role:** check that a parent is refused too, or only the teacher (as M14 wrote)? **Recommend: teacher only. The same `[Authorize(Roles = "Admin")]` guards both.**
