# UI redesign M5 — `ui-parent`: My children, child overview

## Summary
Milestone M5 (handoff P2-9, P2-10; SPEC Decision 4). My children: each child is one whole-row link whose text starts with the child's name (fixes the "View" label-in-name problem). Child overview: a one-line answer under the header ("All 3 classes are on track." / "1 of 4 classes needs attention: ELECT3." with links to the cards / "The early-warning check hasn't run yet this term."), at-risk classes listed first, attendance as four labelled numbers with Absent emphasised, and risk badges through `_StatusBadge` (only "At risk" filled). The disclaimer, switcher, captions and tap targets are unchanged.

## Projects touched
WEB MVC only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Areas/Parent/Views/Children/Index.cshtml` | modified | `list-group-item-action` row links named after the child; "View" button removed |
| `src/Klasly.Web/Areas/Parent/Views/Children/Details.cshtml` | modified | Summary line, at-risk first, labelled attendance numbers, `_StatusBadge` risk badges (no `fs-6`) |
| `src/Klasly.Web/wwwroot/css/site.css` | modified | `.k-answer` (18 px semibold lead line), `.k-stats` (wrapping number group) |

## Key code
```cshtml
var classes = o.Classes.OrderByDescending(c => c.IsAtRisk == true).ToList(); // stable: API order kept inside each group
var atRisk = classes.Where(c => c.IsAtRisk == true).ToList();
var lead = classes.Count == 1 ? "This class needs attention:"
    : atRisk.Count == classes.Count ? $"All {classes.Count} classes need attention:"
    : $"{atRisk.Count} of {classes.Count} classes {(atRisk.Count == 1 ? "needs" : "need")} attention:";
```
No thresholds in the view: the sentence only counts the API's `IsAtRisk` values.

## Database changes
None.

## How it was verified
- `dotnet build src/Klasly.Web`: 0 warnings.
- All three parents (Marites, Rodel, Linda), all six children: the summary matches the cards (local data: every class is at risk, e.g. "All 3 classes need attention: CPE 401, ELECT3, IT302.", "This class needs attention: ELECT3."). The mixed "on track + at risk" ordering couldn't be shown with the local data; it's a one-line stable LINQ sort.
- 375 px screenshot: header, switcher pills, red answer line, card with filled "At risk", labelled numbers.
- My children and both overviews fit at 320/375/768/1440 px.
- The answer-line links first used `link-danger` (#dc3545, about 4.1:1 on the canvas); changed to `text-danger-emphasis` for AA.

## Known limits / TODO
- The page still doesn't say *why* a class is at risk (thresholds aren't in the overview DTO; not hard-coded on purpose).
