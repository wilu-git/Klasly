# UI redesign M7 — `ui-auth-polish`: login page

## Summary
Milestone M7 (handoff P3-1; P3-9 was done in M2). The login page is a centered white panel on the canvas with the Klasly mark and wordmark, the line "Attendance and grades for your classes.", flash messages inside the panel, and a "Logging in…" pending state. The site header is hidden on this page only, so the logo isn't shown twice. Field names, `ReturnUrl`, autocomplete attributes and validation are unchanged.

## Projects touched
WEB MVC only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Views/Account/Login.cshtml` | modified | `.k-panel` with inline mark + wordmark, tagline, `_Alerts` inside the panel (`AlertsInPage`), `data-pending-text="Logging in…"`, students note under the panel |
| `src/Klasly.Web/Views/Shared/_Layout.cshtml` | modified | `ViewData["HideHeader"]` hides the header (used by Login only) |

## Key code
```cshtml
ViewData["AlertsInPage"] = true; // "Your session ended…" lines up with the panel
ViewData["HideHeader"] = true;   // the panel carries the logo
<header @(ViewData["HideHeader"] is true ? "hidden" : "")>
```

## Database changes
None.

## How it was verified
- `dotnet build src/Klasly.Web`: 0 warnings.
- Form contract for `/Account/Login`: identical (Email, Password, ReturnUrl, token).
- Wrong password → "Invalid email or password." inside the panel; the button showed "Logging in…" and was disabled while posting.
- Fits at 320/375/768/1440 px; header hidden only on the login page.

## Known limits / TODO
None.
