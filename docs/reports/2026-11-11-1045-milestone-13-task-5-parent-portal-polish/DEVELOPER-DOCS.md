# Developer docs — Milestone 13 Task 5: Parent portal polish (/impeccable)

## Summary
An `/impeccable polish` pass on the parent pages: a narrow refinement on the existing Bootstrap look (the project has no PRODUCT.md / DESIGN.md, so the incumbent Teacher area is the authority). The pass measured the rendered page at 375 px, 480 px and 1280 px and fixed three defects:
- tap targets on the expandable tables were too small;
- the child pills had uneven heights;
- absences didn't stand out in the records.

The detector reported no issues.

## Projects touched
WEB MVC.

## Files changed
| Path | Added/modified | Why |
| --- | --- | --- |
| `src/Klasly.Web/Areas/Parent/Views/Children/Details.cshtml` | modified | `overview-toggle` on both `<summary>`; a border on both pills (the active one `border-primary`); Absent cells `text-danger-emphasis fw-semibold`; "Not graded yet" `text-body-secondary`. |
| `src/Klasly.Web/wwwroot/css/site.css` | modified | `.overview-toggle { padding-block: .75rem }`. |

## Key code
```html
<summary class="overview-toggle">Attendance records (@c.Attendance.Count)</summary>
...
<td class="@(a.Status == "Absent" ? "text-danger-emphasis fw-semibold" : "")">@a.Status</td>
```
The status keeps its text ("Absent"), so the colour is extra emphasis, not the only signal.

## Database changes
None.

## How it was verified
The browser pane was hidden, so screenshots couldn't render. Measurements were taken with DOM checks on the live page (parent Marites, Aira's page):

| Check | Before | After |
| --- | --- | --- |
| Horizontal overflow at 375 / 480 / 1280 px | none | none |
| `<summary>` height on a phone | 21 px | 42 px |
| Pill heights | 35 / 37 px | 37 / 37 px |
| Badge contrast (white on danger/success) | 4.53:1 | 4.53:1 (AA) |
| Heading order | h1 child, h2 per class | unchanged |
| Absent cells emphasized | 0 | 15 (2 + 12 + 1 across the three classes) |

- `impeccable detect --json` on both views → `[]`.
- Console: no errors from these pages. The single 503 came from a login attempt while the API was still starting.
- `dotnet build Klasly.slnx` → Build succeeded.

## Known limits / TODO
- The one-child redirect wasn't exercised live: every seeded parent has two children, and I didn't change the dev data to test it. It's a single `Count == 1` branch in `ChildrenController.Index`. To check it, unlink one child in Admin › Accounts, log in as that parent, then link the child back.
- The pills are 37 px tall, under the 44 px touch guideline but taller than the Teacher area's `btn-sm` controls (≈31 px). Kept for consistency with the house style.
- A newer Impeccable (v4.8.2) is available (`npx impeccable update`). Not run.
- No PRODUCT.md / DESIGN.md; `/impeccable init` could capture them later.
