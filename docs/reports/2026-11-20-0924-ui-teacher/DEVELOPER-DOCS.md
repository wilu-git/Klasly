# UI redesign M4 — `ui-teacher`: My classes, class page, session page, grade sheet

## Summary
Milestone M4 (handoff P1-4, P1-5, P1-6, P2-14, P3-3, P3-4). The teacher class page now puts a session in progress first (pale-teal strip with **Show QR**) and folds the Start form behind "Start another session" while one is live. Each of the class page's two forms shows its own errors only. After a correction the session page opens at the corrected student's row. The session page shows a large checked-in count and explains what to do when a session is past its end but still open. The grade sheet has a sticky Save bar with a live "N of M graded" count, warns before leaving with unsaved scores, and no longer mentions phone notifications. My classes: the class name is the link, and secondary columns move under it on phones. Every status badge now renders through `_StatusBadge`.

## Projects touched
WEB MVC only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Infrastructure/ModelStateExtensions.cs` | modified | `MoveSummaryErrorsTo(key)`: moves "" errors under a form-specific key |
| `src/Klasly.Web/Areas/Teacher/Controllers/SessionsController.cs` | modified | Start: summary errors → `"start"`; Correct: redirect with fragment `row-{studentId}` |
| `src/Klasly.Web/Areas/Teacher/Controllers/AssessmentsController.cs` | modified | Create: summary errors → `"assessment"` |
| `Areas/Teacher/Views/Shared/_StartSession.cshtml` | modified | Shows only `"start"` errors; "Starting…" pending text |
| `Areas/Teacher/Views/Shared/_AssessmentForm.cshtml` | modified | Add mode shows only `"assessment"` errors, Edit keeps ModelOnly; wider button (`col-md-2`, Title `col-md-3`) |
| `Areas/Teacher/Views/Classes/Index.cshtml` | modified | Class name link; Term/Schedule/Room hidden below md and shown under the name; "Sessions" button removed |
| `Areas/Teacher/Views/Classes/Details.cshtml` | modified | Live strip first; Start form in `<details>` while live (opens on errors); weights line only with assessments, "· complete" at 100 %; row buttons outline-secondary with row context |
| `Areas/Teacher/Views/Sessions/Details.cshtml` | modified | "Press End session to close it and correct results." when Open but past its end |
| `Areas/Teacher/Views/Sessions/_Live.cshtml` | modified | Large "N / M checked in" + breakdown; `<tr id="row-{id}">`; badges via `_StatusBadge`; Save outline-secondary. Single root, `data-closed`/`data-summary`, QR box unchanged |
| `Areas/Teacher/Views/Assessments/Details.cshtml` | modified | Push sentence removed; sticky `.k-savebar` with live count; `data-warn-unsaved` |
| `src/Klasly.Web/wwwroot/css/site.css` | modified | `.k-live`, `.k-summary-btn`, `.k-count`, `.k-savebar`, smaller badge inside `h1` |

## Key code
```csharp
public static void MoveSummaryErrorsTo(this ModelStateDictionary modelState, string key)
{
    if (!modelState.TryGetValue(string.Empty, out var entry) || entry.Errors.Count == 0) return;
    foreach (var error in entry.Errors.ToList()) modelState.AddModelError(key, error.ErrorMessage);
    modelState.Remove(string.Empty);
}
// SessionsController.Correct
return RedirectToAction(nameof(Details), null, new { id }, $"row-{studentId}");
```

## Database changes
None.

## How it was verified
- `dotnet build src/Klasly.Web`: 0 warnings, 0 errors.
- **Form contract:** all six teacher baseline pages (class page, open/ended sessions, both grade sheets) post identical actions, methods and input names.
- **Responsive:** My classes, class page, open and ended session, grade sheet fit at 320/375/768/1440 px (no sideways page scroll).
- Class page with an open session: strip "Session in progress · Started 9:00 AM · ends 10:30 AM (Manila)" + Show QR above everything; Start form behind "Start another session".
- Start with an end time in the past → "The end time must be after the start time." only in the Start form (details reopened); Add assessment over 100 % → message only in the Add form.
- Correct on ended session 1 → URL `/Teacher/Sessions/Details/1#row-4`, flash "No change: Dennis Orpilla is already Absent.", badges subtle danger.
- Open session 2: QR shown, "1 / 6 checked in · 1 Present · 0 Late", `Live/2` polled with 200.
- Grade sheet: typing a score changes the count 0 → 1; a `beforeunload` while dirty is cancelled (warning); no "phone is notified" text; Save bar is `position: sticky`.

## Known limits / TODO
- The reconnect warning (API stopped during a live session) wasn't re-tested; the poller script is unchanged.
- Double-click on Start session relies on the shared guard tested in M2 (button disabled after the first valid submit); no extra session was created to prove it.
