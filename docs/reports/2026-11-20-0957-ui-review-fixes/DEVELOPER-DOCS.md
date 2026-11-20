# UI redesign — review of the branch and two fixes

## Summary
A review of the whole `ui-redesign` diff against `dev` (33 view/CSS/JS files, 5 server files). It checked the riskiest paths: re-executed status pages, the submit and unsaved guards, partial rendering in the polled live panel, and the layout. Two real problems were fixed: the unsaved-changes warning was lost when a form came back from the server with errors, and the class-list term filter relied on `requestSubmit()`, which older Safari lacks while the Show button is hidden when JavaScript runs.

## Projects touched
WEB MVC only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/wwwroot/js/site.js` | modified | A `data-warn-unsaved` form that renders with server errors (`.field-validation-error`, `.validation-summary-errors`) starts as dirty: its typed values aren't saved |
| `src/Klasly.Web/Areas/Admin/Views/Classes/Index.cshtml` | modified | `onchange="this.form.submit()"` instead of `requestSubmit()` (works in every browser; it's a GET filter, so skipping the submit event is harmless) |

## Review results (no change needed)
- **Re-executed POSTs:** no global antiforgery filter exists, so a re-executed POST reaches `/Home/StatusCode`. A POST with a bogus token now returns **400** with the styled Error page and navigation (verified).
- **Live poll:** the polled `_Live` partial renders `_StatusBadge` from `Views/Shared` (badges appear after each 5 s swap); any non-OK response still shows "Reconnecting…".
- **Submit guard order:** jQuery Validation's handler runs on the form before the document-level handler, so `defaultPrevented` correctly skips invalid forms; native constraint failures never fire `submit`.
- **Account page:** Link/Unlink submits set `data-submitted` on their own form only, so typed edits in the account form still trigger the leave warning (intended).
- **Server footprint:** 36 added lines, all within SPEC Decision 2.

## Database changes
None.

## How it was verified
- Grade sheet: posted 999 for a 100-point assessment without client validation → server error "Score must be between 0 and 100.", typed value kept, form `data-dirty="true"` on load.
- Classes list: choosing "All terms" in the filter → `?termId=0`, list reloaded.
- Bogus antiforgery token on `POST /Admin/Terms/Create` → 400 "Something went wrong - Klasly" with navbar.
- `dotnet build src/Klasly.Web`: 0 warnings (run before commit).

## Known limits / TODO
- The 400 page reuses the generic "Something went wrong on our side." copy; a stale form is usually the user's old tab, not a server fault. Left as is (message pages are shared by design).
