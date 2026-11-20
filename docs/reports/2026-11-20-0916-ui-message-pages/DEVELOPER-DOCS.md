# UI redesign M3 — `ui-message-pages`: Not found, Error, Access denied, Server unavailable

## Summary
Milestone M3 (SPEC.md Decision 2 / handoff P1-1, P1-2, P2-4). Error status codes with no body (for example an API 404 for a bad id) used to show a plain-text status page with no navigation; they now re-run `/Home/StatusCode` inside the normal layout and show a styled **Not found** page, still with HTTP 404. The template **Error** page that told users about `ASPNETCORE_ENVIRONMENT` is replaced by a calm message with a "Reference" id. **Access denied** has correct copy for role refusals and shows the API's reason once (no duplicate red alert). **Server unavailable** has user-facing copy and a real button. All four share one message-page pattern.

## Projects touched
WEB MVC only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/Program.cs` | modified | `UseStatusCodePages()` → `UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}")` |
| `src/Klasly.Web/Controllers/HomeController.cs` | modified | New anonymous, no-store `StatusCode` action: 404 → NotFound view, other 4xx/5xx → Error view, original status kept |
| `src/Klasly.Web/Views/Shared/NotFound.cshtml` | added | "We couldn't find that page." + home button |
| `src/Klasly.Web/Views/Shared/Error.cshtml` | modified | User-facing message, small "Reference: {RequestId}"; Development Mode text removed |
| `src/Klasly.Web/Views/Shared/ApiUnavailable.cshtml` | modified | "tell the school admin" instead of "check the Web API"; `<button type="button">` instead of `<a href="">` |
| `src/Klasly.Web/Views/Account/AccessDenied.cshtml` | modified | Copy correct for role refusals; API reason rendered in the page (`AlertsInPage`) |
| `src/Klasly.Web/wwwroot/css/site.css` | modified | `.k-message` (36 rem max width, vertical padding) |

## Key code
```csharp
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

[AllowAnonymous, ActionName("StatusCode")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public IActionResult StatusCodePage(int code)
{
    var status = code is >= 400 and <= 599 ? code : StatusCodes.Status404NotFound;
    var result = status == StatusCodes.Status404NotFound ? View("NotFound") : View("Error", new ErrorViewModel { RequestId = ... });
    result.StatusCode = status;
    return result;
}
```
The method is named `StatusCodePage` because `ControllerBase.StatusCode(int)` already exists; `[ActionName]` keeps the route `/Home/StatusCode`.

## Database changes
None.

## How it was verified
- `/Admin/Classes/Details/999999` (API 404) → **404**, title "Not found - Klasly", navbar present. `/does/not/exist` → 404 Not found. `/Home/StatusCode?code=500` → 500 Error page; `?code=abc` → 404 Not found.
- Teacher opening `/Admin` → Access denied: "Your account doesn't have access to it. If you think this is wrong, ask the school admin." Parent opening another child (`/Parent/Children/Details/3`) → Access denied with the API reason "You can only view your own children" shown once, no red alert.
- API stopped → `/Parent/Children` returns 503 "Klasly can't reach the server right now." with `<button type="button">Try again</button>`.
- `/Home/Error` → no "Development" text, shows "Reference: 00-…".
- The live-session poller treats any non-OK response as "Reconnecting…" (`if (!res.ok) throw`), so re-executed HTML bodies don't affect it.
- `dotnet build src/Klasly.Web`: 0 warnings. `dotnet test Klasly.slnx`: 163 passed.

## Known limits / TODO
- The production exception path (`UseExceptionHandler`) only runs outside Development; checked by rendering `/Home/Error` directly.
