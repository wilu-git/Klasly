# Developer docs: Failed emails page checks (`/impeccable` pass + browser check)

## Summary
This closes the two boxes Milestone 14 Task 8 left open: the `/impeccable` pass on `Admin › Failed emails`, and the browser check.
- **Browser check:** an admin sees the list, a teacher is refused, and an unreachable API shows the house "can't reach the server" page.
- **`/impeccable` pass:** found one defect. Below about 500 px the Last error and Alert columns collapsed, so a long Brevo error became a 39-line row.
- **The fix:** one CSS rule. The table now scrolls inside its box on phones instead.

Spec: `SPEC.md`. Plan: `tasks/plan-failed-emails-checks.md`. Task details and every measurement: `tasks/todo-failed-emails-checks.md`.

## Projects touched
WEB MVC.

## Files changed
| Path | Added/modified/deleted | Why |
| --- | --- | --- |
| `src/Klasly.Web/wwwroot/css/site.css` | modified | New `.cell-readable { min-width: 12rem; }` rule |
| `src/Klasly.Web/Areas/Admin/Views/FailedEmails/Index.cshtml` | modified | `cell-readable` on the Alert and Last error cells |
| `SPEC.md` | modified | Spec for this follow-up (replaces M15's, which is in git history at `6522027`); all criteria ticked |
| `tasks/plan-failed-emails-checks.md`, `tasks/todo-failed-emails-checks.md` | added | Plan and task list with the measurements |
| `tasks/todo-m14.md` | modified | Task 8's two open boxes ticked |
| `docs/reports/2026-11-11-1605-failed-emails-page-checks/*.jpg` | added | 5 screenshots (listed under "How it was verified") |

## Key code
```css
/* Long text cells keep a readable width on phones; the table scrolls inside .table-responsive instead of squeezing them. */
.cell-readable {
  min-width: 12rem;
}
```
```html
<td class="cell-readable"> ...alert type + subject... </td>
<td class="small text-break cell-readable">@(email.LastError ?? "—")</td>
```
**Why it happened:** `text-break` sets `word-break: break-word`. That lets the browser shrink the cell to the width of a single character whenever the table runs short of room, so the auto table layout squeezed the column the admin actually needs.

**Why the fix works:** a minimum width stops the squeeze, and `table-responsive` (already in place, as on every Admin table) scrolls the extra width. `12rem` is 168 px on phones (the root font is 14 px there) and 192 px on desktop.

## Database changes
None.

## How it was verified
**Browser check** (built-in browser pane, dev database, seeded fictional accounts):

| Check | Result | Screenshot |
| --- | --- | --- |
| Admin opens `/Admin/FailedEmails` | Lists the M12 "Brevo is not configured" rows, each with Manila time, recipient, alert, attempts and last error | `1-admin-list.jpg` |
| Teacher (`rosa.villanueva@klasly.test`) types the URL | Access denied page, no data | `2-teacher-refused.jpg` |
| Signed out | Redirect to login with `ReturnUrl=/Admin/FailedEmails` | (none) |
| API stopped | "Klasly can't reach the server right now." (`ApiUnavailable`, HTTP 503, from the shared `ApiExceptionFilter`) | `3-api-down.jpg` |
| Console | No errors from the page. The only 503 entry was the API-down test itself. Every request on a normal load returned 200 | (none) |

**`/impeccable` measurements:** a ~200-character Brevo error was injected into the DOM for the test, then restored.

| Measure | 375 px before → after | 480 px before → after | 1280 px before → after |
| --- | --- | --- | --- |
| Long Brevo error | **39 → 9 lines**, row 732 → 180 px | 39 → 9 lines, 732 → 180 px | 3 → 3 lines, 83 → 83 px |
| "Brevo is not configured" | 4 → 1 line | 4 → 1 line | 1 → 1 line |
| Alert subject | 5 → 2 lines | 5 → 2 lines | 1 → 1 line |
| Row height | 128 → 73 px | 128 → 73 px | 62 → 62 px |
| Page scrolls sideways | no → no | no → no | no → no |

Phone screenshots: `4-phone-375-before.jpg`, `5-phone-375-after.jpg`.

**Other checks:**
- Contrast: main text 21:1, `text-body-secondary` ≈ 6.7:1. Both pass AA.
- Structure: one `h1`, `scope="col"` on every header, `aria-current` on the nav item, `lang="en"`.
- `impeccable detect --json` on the view and `site.css`: `[]`.

**Build and tests:**
- `dotnet build Klasly.slnx` → 0 warnings, 0 errors.
- `dotnet test Klasly.slnx` → 162 passed, 0 failed, 0 skipped.
- `graphify update .` run.

## Known limits / TODO
- **Empty state not seen live.** "No failed emails. Every alert reached Brevo." was checked in the markup only, because showing it needs the dev data changed.
- **1280 px column widths moved slightly.** Rows and line counts are identical, but the browser shares out the spare width differently: Last error +33 px, other columns 3–15 px less. Accepted; a media query to undo it would add code for no visible gain.
- **The recipient email doesn't wrap, on purpose.** It stays whole so it's easy to read and copy. The spec criterion was corrected to say so.
- **No real Brevo error text seen.** Every dev row shows 0 attempts and "Brevo is not configured", so the long-error case was tested with injected text.
- **The "—" for no error is never reached in practice.** The Worker always sets `LastError` when it marks a row Failed (`EmailSender.cs:60`, `:79`).
- **No PRODUCT.md / DESIGN.md.** `/impeccable init` could capture them later.
